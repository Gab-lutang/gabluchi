using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;
using System.Threading;
using System.Threading.Tasks;
using GabLuchi.Models;

namespace GabLuchi.Services;

public class HubcapService
{
	private readonly HttpClient _http = new HttpClient
	{
		BaseAddress = new Uri("https://hubcapmanifest.com"),
		Timeout = TimeSpan.FromMinutes(5.0)
	};

	private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true
	};

	private static readonly string InterimDownloadsFolder = Path.Combine(Path.GetTempPath(), "GabLuchi", "downloads");

	private static Regex KeyFormatRegex()
	{
		return _003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__KeyFormatRegex_2.Instance;
	}

	public static bool IsValidKeyFormat(string? key)
	{
		if (key != null)
		{
			return KeyFormatRegex().IsMatch(key);
		}
		return false;
	}

	public async Task<HubcapStats?> GetStatsAsync(string key, CancellationToken ct = default(CancellationToken))
	{
		_ = 1;
		try
		{
			HttpResponseMessage httpResponseMessage = await _http.GetAsync("/api/v1/user/stats?api_key=" + Uri.EscapeDataString(key), ct);
			if (!httpResponseMessage.IsSuccessStatusCode)
			{
				return null;
			}
			return await ReadJsonAsync<HubcapStats>(httpResponseMessage, ct);
		}
		catch
		{
			return null;
		}
	}

	public async Task<HubcapManifestStatus?> CheckStatusAsync(string key, string appid, CancellationToken ct = default(CancellationToken))
	{
		_ = 1;
		try
		{
			HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "/api/v1/status/" + Uri.EscapeDataString(appid));
			httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
			HttpResponseMessage httpResponseMessage = await _http.SendAsync(httpRequestMessage, ct);
			if (!httpResponseMessage.IsSuccessStatusCode)
			{
				return null;
			}
			return await ReadJsonAsync<HubcapManifestStatus>(httpResponseMessage, ct);
		}
		catch
		{
			return null;
		}
	}

	public async Task<DownloadedFile> DownloadManifestAsync(string appid, string key, IProgress<double?>? progress, CancellationToken ct = default(CancellationToken))
	{
		string requestUri = "/api/v1/manifest/" + Uri.EscapeDataString(appid) + "?api_key=" + Uri.EscapeDataString(key);
		using HttpResponseMessage res = await _http.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, ct);
		if (!res.IsSuccessStatusCode)
		{
			throw new ApiException(res.StatusCode switch
			{
				HttpStatusCode.Unauthorized => "Your Hubcap key is invalid or expired.", 
				HttpStatusCode.TooManyRequests => "Your Hubcap daily limit has been reached.", 
				HttpStatusCode.NotFound => "No Hubcap manifest is available for this app.", 
				_ => $"Hubcap download failed ({(int)res.StatusCode}).", 
			}, res.StatusCode);
		}
		return await SaveResponseAsync(res, appid + ".zip", progress, ct);
	}

	private static async Task<T?> ReadJsonAsync<T>(HttpResponseMessage res, CancellationToken ct)
	{
		return JsonSerializer.Deserialize<T>(await res.Content.ReadAsStringAsync(ct), JsonOpts);
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
