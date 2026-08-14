using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
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
			return (TopSellers: Clean(steamFeaturedResponse?.TopSellers), NewReleases: Clean(steamFeaturedResponse?.NewReleases));
		}
		catch
		{
			return (TopSellers: new List<SteamFeaturedItem>(), NewReleases: new List<SteamFeaturedItem>());
		}
		static List<SteamFeaturedItem> Clean(SteamFeaturedCategory? c)
		{
			return (c?.Items ?? new List<SteamFeaturedItem>()).Where((SteamFeaturedItem i) => i.Type == 0 && i.Id > 0 && !string.IsNullOrEmpty(i.LargeCapsuleImage)).DistinctBy((SteamFeaturedItem i) => i.Id).Take(20)
				.ToList();
		}
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
