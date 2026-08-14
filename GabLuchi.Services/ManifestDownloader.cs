using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GabLuchi;
using GabLuchi.Models;
using GabLuchi.Resources;

namespace GabLuchi.Services;

public class ManifestDownloader
{
	private const string AppIdToken = "<appid>";

	private static readonly string InterimDownloadsFolder = Path.Combine(Path.GetTempPath(), "GabLuchi", "downloads");

	private readonly HttpClient _http = new HttpClient
	{
		Timeout = TimeSpan.FromMinutes(10.0)
	};

	private readonly AuthService _auth;

	private readonly LicenseService _license;

	private List<ApiSource>? _sources;

	private bool _sourcesLoaded;

	public ManifestDownloader(AuthService auth, LicenseService license)
	{
		_auth = auth;
		_license = license;
	}

	public string? ResolveSourceUrl(string source)
	{
		return ResolveSource(source)?.Url;
	}

	private ApiSource? ResolveSource(string source)
	{
		LoadSources();
		return _sources?.FirstOrDefault((ApiSource s) => string.Equals(s.Name, source, StringComparison.OrdinalIgnoreCase));
	}

	public async Task<DownloadedFile> DownloadManifestAsync(string appid, string source, string? gameName, IProgress<double?>? progress, CancellationToken ct = default(CancellationToken))
	{
		ApiSource? resolved = ResolveSource(source);
		if (resolved == null || string.IsNullOrWhiteSpace(resolved.Url))
		{
			throw new ApiException($"No download URL configured for source '{source}'.");
		}
		string? licenseUrl = _license.GetDownloadUrl(appid);
		if (licenseUrl == null)
		{
			throw new ApiException(Strings.Add_Err_LicenseRequired);
		}
		HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, licenseUrl);
		if (resolved.RequiresAuth)
		{
			string token = await _auth.GetValidAccessTokenAsync();
			request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
		}
		using HttpResponseMessage res = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
		if (!res.IsSuccessStatusCode)
		{
			throw new ApiException($"Download failed ({(int)res.StatusCode}) from source '{source}'.", res.StatusCode);
		}
		string downloadUrl = await ReadUrlAsync(res);
		if (string.IsNullOrWhiteSpace(downloadUrl))
		{
			throw new ApiException($"Download failed (no URL) from source '{source}'.", res.StatusCode);
		}
		using HttpRequestMessage fileRequest = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
		HttpResponseMessage fileRes = await _http.SendAsync(fileRequest, HttpCompletionOption.ResponseHeadersRead, ct);
		if (!fileRes.IsSuccessStatusCode)
		{
			throw new ApiException($"Download failed ({(int)fileRes.StatusCode}) from source '{source}'.", fileRes.StatusCode);
		}
		return await SaveResponseAsync(fileRes, appid + ".zip", progress, ct);
	}

	private static async Task<string> ReadUrlAsync(HttpResponseMessage res)
	{
		try
		{
			string body = await res.Content.ReadAsStringAsync();
			using JsonDocument doc = JsonDocument.Parse(body);
			if (doc.RootElement.TryGetProperty("url", out JsonElement e) && e.ValueKind == JsonValueKind.String)
			{
				return e.GetString() ?? "";
			}
		}
		catch
		{
		}
		return "";
	}

	private static async Task<DownloadedFile> SaveResponseAsync(HttpResponseMessage res, string fallbackName, IProgress<double?>? progress, CancellationToken ct)
	{
		string fileName = res.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? fallbackName;
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		foreach (char oldChar in invalidFileNameChars)
		{
			fileName = fileName.Replace(oldChar, '_');
		}
		Directory.CreateDirectory(InterimDownloadsFolder);
		string filePath = Path.Combine(InterimDownloadsFolder, fileName);
		long? total = res.Content.Headers.ContentLength;
		await using (Stream src = await res.Content.ReadAsStreamAsync(ct))
		{
			await using (FileStream dst = File.Create(filePath))
			{
				byte[] buffer = new byte[81920];
				long written = 0L;
				while (true)
				{
					int read = await src.ReadAsync(buffer, ct);
					if (read <= 0)
					{
						break;
					}
					await dst.WriteAsync(buffer.AsMemory(0, read), ct);
					written += read;
					progress?.Report((total.HasValue && total.GetValueOrDefault() > 0) ? new double?((double)written / (double)total.Value) : ((double?)null));
				}
			}
		}
		return new DownloadedFile(filePath, fileName);
	}

	private void LoadSources()
	{
		if (_sourcesLoaded)
		{
			return;
		}
		_sourcesLoaded = true;
		string[] array = new string[2]
		{
			Path.Combine(AppContext.BaseDirectory, "public", "api.json"),
			Path.Combine(AppContext.BaseDirectory, "api.json")
		};
		foreach (string path in array)
		{
			if (!File.Exists(path))
			{
				continue;
			}
			try
			{
				using JsonDocument jsonDocument = JsonDocument.Parse(File.ReadAllText(path));
				if (jsonDocument.RootElement.TryGetProperty("api_list", out var value))
				{
					List<ApiSource> list = new List<ApiSource>();
					foreach (JsonElement item in value.EnumerateArray())
					{
						JsonElement value2;
						string name = (item.TryGetProperty("name", out value2) ? (value2.GetString() ?? "") : "");
						JsonElement value3;
						string url = (item.TryGetProperty("url", out value3) ? (value3.GetString() ?? "") : "");
						JsonElement value4;
						int successCode = (item.TryGetProperty("success_code", out value4) ? value4.GetInt32() : 200);
						JsonElement value5;
						bool enabled = !item.TryGetProperty("enabled", out value5) || value5.GetBoolean();
						JsonElement value6;
						bool requiresAuth = item.TryGetProperty("auth", out value6) && value6.GetBoolean();
						if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(url) && enabled)
						{
							list.Add(new ApiSource(name, url, successCode, requiresAuth));
						}
					}
					if (list.Count > 0)
					{
						_sources = list;
						return;
					}
				}
			}
			catch
			{
			}
		}
		_sources = new List<ApiSource>
		{
			new ApiSource("Ryuu", "http://167.235.229.108/<appid>", 200),
			new ApiSource("Sushi", "https://raw.githubusercontent.com/sushi-dev55-alt/sushitools-games-repo-alt/refs/heads/main/<appid>.zip", 200),
			new ApiSource("Luie", "http://167.235.229.108/<appid>", 200)
		};
	}
}
