using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GabLuchi;
using GabLuchi.Models;

namespace GabLuchi.Services;

public class GabLuchiApiClient(SteamAppInfoCache appInfo, CoverCache covers)
{
	private static readonly string InterimDownloadsFolder = Path.Combine(Path.GetTempPath(), "GabLuchi", "downloads");

	private readonly HttpClient _http = new HttpClient
	{
		BaseAddress = new Uri(Config.ApiBaseUrl),
		Timeout = TimeSpan.FromMinutes(5.0)
	};

	private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true
	};

	public async Task<List<SteamSearchResult>> SearchAsync(string query, CancellationToken ct = default(CancellationToken))
	{
		string requestUri = "https://store.steampowered.com/api/storesearch/?term=" + Uri.EscapeDataString(query) + "&l=english&cc=US";
		HttpResponseMessage httpResponseMessage = await _http.GetAsync(requestUri, ct);
		if (!httpResponseMessage.IsSuccessStatusCode)
		{
			return new List<SteamSearchResult>();
		}
		return (from i in ((await ReadJsonAsync<SteamStoreSearchResponse>(httpResponseMessage, ct))?.Items ?? new List<SteamStoreItem>()).Take(8)
			select new SteamSearchResult
			{
				AppId = i.Id,
				Name = i.Name,
				Icon = (i.TinyImage ?? $"https://cdn.cloudflare.steamstatic.com/steam/apps/{i.Id}/capsule_sm_120.jpg")
			}).ToList();
	}

	public async Task<(List<SteamFeaturedItem> TopSellers, List<SteamFeaturedItem> NewReleases)> GetFeaturedAsync(CancellationToken ct = default(CancellationToken))
	{
		_ = 1;
		try
		{
			HttpResponseMessage httpResponseMessage = await _http.GetAsync("https://store.steampowered.com/api/featuredcategories?cc=us&l=english", ct);
			if (!httpResponseMessage.IsSuccessStatusCode)
			{
				return (TopSellers: new List<SteamFeaturedItem>(), NewReleases: new List<SteamFeaturedItem>());
			}
			SteamFeaturedResponse steamFeaturedResponse = await ReadJsonAsync<SteamFeaturedResponse>(httpResponseMessage, ct);
			return (TopSellers: Clean(steamFeaturedResponse?.TopSellers, 50), NewReleases: Clean(steamFeaturedResponse?.NewReleases, 20));
		}
		catch
		{
			return (TopSellers: new List<SteamFeaturedItem>(), NewReleases: new List<SteamFeaturedItem>());
		}
		static List<SteamFeaturedItem> Clean(SteamFeaturedCategory? c, int max)
		{
			return (c?.Items ?? new List<SteamFeaturedItem>()).Where((SteamFeaturedItem i) => i.Type == 0 && i.Id > 0 && !string.IsNullOrEmpty(i.LargeCapsuleImage)).DistinctBy((SteamFeaturedItem i) => i.Id).Take(max)
				.ToList();
		}
	}

	public async Task<List<SteamFeaturedItem>> GetTopSellersAsync(CancellationToken ct = default(CancellationToken))
	{
		try
		{
			const string requestUri = "https://store.steampowered.com/search/results/?json=1&filter=globaltopsellers&norender=1&cc=us&l=english";
			HttpResponseMessage httpResponseMessage = await _http.GetAsync(requestUri, ct);
			if (!httpResponseMessage.IsSuccessStatusCode)
			{
				return new List<SteamFeaturedItem>();
			}
			SteamSearchFeedResponse? steamSearchFeedResponse = await ReadJsonAsync<SteamSearchFeedResponse>(httpResponseMessage, ct);
			List<(long Id, string Name, string Logo)> parsed = (steamSearchFeedResponse?.Items ?? new List<SteamSearchFeedItem>())
				.Select((SteamSearchFeedItem i) => new
				{
					Id = ExtractFeedAppId(i.Logo),
					Item = i
				})
				.Where(t => t.Id > 0)
				.DistinctBy(t => t.Id)
				.Take(50)
				.Select(t => (t.Id, t.Item.Name, t.Item.Logo))
				.ToList();
			if (parsed.Count == 0)
			{
				return new List<SteamFeaturedItem>();
			}
			Dictionary<long, TopSellersRowInfo> rows = await FetchTopSellersRowMarkersAsync(ct);
			List<SteamFeaturedItem> filtered = new List<SteamFeaturedItem>();
			foreach ((long Id, string Name, string Logo) item in parsed)
			{
				if (!rows.TryGetValue(item.Id, out TopSellersRowInfo row))
				{
					filtered.Add(new SteamFeaturedItem
					{
						Id = item.Id,
						Name = item.Name,
						LargeCapsuleImage = item.Logo,
						Type = 0
					});
					continue;
				}
				if (row.IsFree)
				{
					continue;
				}
				if (row.ItemKey != null && !string.Equals(row.ItemKey, "App", StringComparison.Ordinal))
				{
					continue;
				}
				if (row.PriceCents >= 50000)
				{
					continue;
				}
				filtered.Add(new SteamFeaturedItem
				{
					Id = item.Id,
					Name = item.Name,
					LargeCapsuleImage = row.Art ?? item.Logo,
					Type = 0
				});
			}
			return filtered;
		}
		catch
		{
			return new List<SteamFeaturedItem>();
		}
		static long ExtractFeedAppId(string? logo)
		{
			if (string.IsNullOrEmpty(logo))
			{
				return 0L;
			}
			Match match = Regex.Match(logo, "/apps/(\\d+)/");
			if (!match.Success || !long.TryParse(match.Groups[1].Value, out long id))
			{
				return 0L;
			}
			return id;
		}
	}

	private async Task<Dictionary<long, TopSellersRowInfo>> FetchTopSellersRowMarkersAsync(CancellationToken ct)
	{
		try
		{
			HttpResponseMessage responseMessage = await _http.GetAsync("https://store.steampowered.com/search/?filter=globaltopsellers&cc=us&l=english", ct);
			if (!responseMessage.IsSuccessStatusCode)
			{
				return new Dictionary<long, TopSellersRowInfo>();
			}
			string html = await responseMessage.Content.ReadAsStringAsync(ct);
			Dictionary<long, TopSellersRowInfo> map = new Dictionary<long, TopSellersRowInfo>();
			foreach (Match match in Regex.Matches(html, "<a [^>]*href=\"https://store\\.steampowered\\.com/app/(\\d+)/[^\"]*\"[^>]*data-ds-appid=\"(\\d+)\"[^>]*>(.*?)</a>", RegexOptions.Singleline))
			{
				long appid = long.Parse(match.Groups[2].Value);
				if (appid != long.Parse(match.Groups[1].Value))
				{
					continue;
				}
				string row = match.Groups[3].Value;
				Match itemKeyMatch = Regex.Match(row, "data-ds-itemkey=\"(App|Sub|Bundle)_");
				string? itemKey = itemKeyMatch.Success ? itemKeyMatch.Groups[1].Value : null;
				Match artMatch = Regex.Match(row, "src=\"([^\"]*capsule_231x87[^\"]*)\"");
				string? art = artMatch.Success ? artMatch.Groups[1].Value : null;
				string priceText = Regex.Replace(row, "<[^>]+>", " ");
				Match priceMatch = Regex.Match(priceText, "\\$[\\d,]+\\.?\\d*");
				bool isFree = priceText.IndexOf("free", StringComparison.OrdinalIgnoreCase) >= 0 && !priceMatch.Success;
				long? priceCents = null;
				if (priceMatch.Success && decimal.TryParse(priceMatch.Value.TrimStart('$').Replace(",", ""), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out decimal value))
				{
					priceCents = (long)(value * 100m);
				}
				map[appid] = new TopSellersRowInfo(isFree, art, itemKey, priceCents);
			}
			return map;
		}
		catch
		{
			return new Dictionary<long, TopSellersRowInfo>();
		}
	}

	private sealed record TopSellersRowInfo(bool IsFree, string? Art, string? ItemKey, long? PriceCents);

	public async Task<List<SteamFeaturedItem>> FilterPaidOnlyAsync(IEnumerable<SteamFeaturedItem> items, CancellationToken ct = default(CancellationToken))
	{
		List<SteamFeaturedItem> list = items.Where((SteamFeaturedItem i) => i.Id > 0).DistinctBy((SteamFeaturedItem i) => i.Id).ToList();
		await appInfo.EnsureFullDetailsBatchAsync(list.Select((SteamFeaturedItem i) => i.Id).ToList(), ct);
		return list.Where((SteamFeaturedItem i) => IsPaidGame(i.Id)).ToList();
	}

	private bool IsPaidGame(long appid)
	{
		AppFilterData? filter = appInfo.GetFilterData(appid);
		if (filter == null)
		{
			return true;
		}
		return !filter.IsFree && (filter.Type == null || string.Equals(filter.Type, "game", StringComparison.Ordinal));
	}

	public async Task<GameDetails?> GetDetailsAsync(string appid, CancellationToken ct = default(CancellationToken))
	{
		if (!long.TryParse(appid, out var id))
		{
			return null;
		}
		GameDetails gameDetails = await appInfo.ResolveGameDetailsAsync(id, ct);
		if (gameDetails != null)
		{
			string headerImage = gameDetails.HeaderImage;
			if (headerImage != null && headerImage.Length > 0)
			{
				covers.EnsureAsync(id, headerImage, CancellationToken.None);
			}
		}
		return gameDetails;
	}

	public async Task<Dictionary<string, string>> CheckSourcesAsync(string appid, CancellationToken ct = default(CancellationToken))
	{
		HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, Config.ManifestBackendBase + "/check_apis?appid=" + appid);
		httpRequestMessage.Headers.TryAddWithoutValidation("User-Agent", Config.ManifestBackendUserAgent);
		HttpResponseMessage httpResponseMessage = await _http.SendAsync(httpRequestMessage, ct);
		if (!httpResponseMessage.IsSuccessStatusCode)
		{
			return new Dictionary<string, string>();
		}
		return (await ReadJsonAsync<Dictionary<string, string>>(httpResponseMessage, ct)) ?? new Dictionary<string, string>();
	}

	public async Task<DlcInfo?> GetDlcInfoAsync(string appid, string baseAppId, CancellationToken ct = default(CancellationToken))
	{
		return await ReadJsonAsync<DlcInfo>(await SendAsync(HttpMethod.Get, "/api/dlc/info?appid=" + appid + "&base=" + baseAppId, ct), ct);
	}

	public Task<DownloadedFile> GenerateDlcAsync(string appid, string baseAppId, string? gameName, IProgress<double?>? progress, CancellationToken ct = default(CancellationToken))
	{
		string text = "/api/dlc/generate?appid=" + appid + "&base=" + baseAppId;
		if (!string.IsNullOrEmpty(gameName))
		{
			text = text + "&game_name=" + Uri.EscapeDataString(gameName);
		}
		return DownloadFileAsync(text, appid + ".lua", progress, ct);
	}

	public async Task<DenuvoListingsResponse?> GetDenuvoListingsAsync(CancellationToken ct = default(CancellationToken))
	{
		HttpResponseMessage httpResponseMessage = await _http.GetAsync("/api/denuvo/listings", ct);
		if (!httpResponseMessage.IsSuccessStatusCode)
		{
			return null;
		}
		return await ReadJsonAsync<DenuvoListingsResponse>(httpResponseMessage, ct);
	}

	public async Task<DenuvoFixesResponse?> GetDenuvoFixesAsync(string appid, CancellationToken ct = default(CancellationToken))
	{
		HttpResponseMessage httpResponseMessage = await _http.GetAsync("/api/denuvo/fixes?appid=" + Uri.EscapeDataString(appid), ct);
		if (!httpResponseMessage.IsSuccessStatusCode)
		{
			return null;
		}
		return await ReadJsonAsync<DenuvoFixesResponse>(httpResponseMessage, ct);
	}

	private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, CancellationToken ct, HttpCompletionOption completion = HttpCompletionOption.ResponseContentRead)
	{
		HttpRequestMessage req = new HttpRequestMessage(method, url);
		HttpResponseMessage res = await _http.SendAsync(req, completion, ct);
		if (res.IsSuccessStatusCode)
		{
			return res;
		}
		string message = $"Request failed ({(int)res.StatusCode})";
		try
		{
			ApiError apiError = JsonSerializer.Deserialize<ApiError>(await res.Content.ReadAsStringAsync(ct), JsonOpts);
			if (!string.IsNullOrWhiteSpace(apiError?.Error))
			{
				message = apiError.Error;
			}
		}
		catch
		{
		}
		if (res.StatusCode == HttpStatusCode.Unauthorized)
		{
			message = "The server rejected the request.";
		}
		throw new ApiException(message, res.StatusCode);
	}

	private static async Task<T?> ReadJsonAsync<T>(HttpResponseMessage res, CancellationToken ct)
	{
		return JsonSerializer.Deserialize<T>(await res.Content.ReadAsStringAsync(ct), JsonOpts);
	}

	private async Task<DownloadedFile> DownloadFileAsync(string url, string fallbackName, IProgress<double?>? progress, CancellationToken ct)
	{
		return await SaveResponseAsync(await SendAsync(HttpMethod.Get, url, ct, HttpCompletionOption.ResponseHeadersRead), fallbackName, progress, ct);
	}

	private async Task<DownloadedFile> SaveResponseAsync(HttpResponseMessage res, string fallbackName, IProgress<double?>? progress, CancellationToken ct)
	{
		string fileName = res.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? fallbackName;
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		foreach (char oldChar in invalidFileNameChars)
		{
			fileName = fileName.Replace(oldChar, '_');
		}
		string interimDownloadsFolder = InterimDownloadsFolder;
		Directory.CreateDirectory(interimDownloadsFolder);
		string filePath = Path.Combine(interimDownloadsFolder, fileName);
		long? total = res.Content.Headers.ContentLength;
		DownloadedFile result;
		await using (Stream src = await res.Content.ReadAsStreamAsync(ct))
		{
			DownloadedFile downloadedFile;
			await using (FileStream dst = File.Create(filePath))
			{
				byte[] buffer = new byte[81920];
				long written = 0L;
				while (true)
				{
					int num;
					int read = (num = await src.ReadAsync(buffer, ct));
					if (num <= 0)
					{
						break;
					}
					await dst.WriteAsync(buffer.AsMemory(0, read), ct);
					written += read;
					progress?.Report((total.HasValue && total.GetValueOrDefault() > 0) ? new double?((double)written / (double)total.Value) : ((double?)null));
				}
				downloadedFile = new DownloadedFile(filePath, fileName);
			}
			result = downloadedFile;
		}
		return result;
	}
}
