using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GabLuchi.Services;

public class GithubProxy
{
	private readonly HttpClient _http = new HttpClient
	{
		Timeout = TimeSpan.FromMinutes(5.0)
	};

	private static bool IsGithub(string url)
	{
		if (!url.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase) && !url.StartsWith("https://api.github.com/", StringComparison.OrdinalIgnoreCase) && !url.StartsWith("https://raw.githubusercontent.com/", StringComparison.OrdinalIgnoreCase))
		{
			return url.StartsWith("https://objects.githubusercontent.com/", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	public static IEnumerable<string> Candidates(string url)
	{
		yield return url;
		if (IsGithub(url))
		{
			string[] array = (url.StartsWith("https://api.github.com/", StringComparison.OrdinalIgnoreCase) ? AppConfig.GithubApiMirrors : AppConfig.GithubDownloadMirrors);
			string[] array2 = array;
			foreach (string text in array2)
			{
				yield return text + url;
			}
		}
	}

	public async Task<HttpResponseMessage?> SendAsync(string url, CancellationToken ct = default(CancellationToken))
	{
		HttpResponseMessage lastResponse = null;
		foreach (string item in Candidates(url))
		{
			try
			{
				using HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, item);
				req.Headers.TryAddWithoutValidation("User-Agent", "GabLuchi");
				req.Headers.TryAddWithoutValidation("Accept", "application/vnd.github+json");
				HttpResponseMessage httpResponseMessage = await _http.SendAsync(req, ct);
				if (httpResponseMessage.IsSuccessStatusCode)
				{
					return httpResponseMessage;
				}
				lastResponse?.Dispose();
				lastResponse = httpResponseMessage;
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				throw;
			}
			catch
			{
			}
		}
		return lastResponse;
	}

	public async Task DownloadAsync(string url, string destPath, IProgress<double?>? progress, CancellationToken ct = default(CancellationToken))
	{
		Exception last = null;
		foreach (string item in Candidates(url))
		{
			try
			{
				using HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, item);
				req.Headers.TryAddWithoutValidation("User-Agent", "GabLuchi");
				using HttpResponseMessage res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
				res.EnsureSuccessStatusCode();
				long? total = res.Content.Headers.ContentLength;
				await using Stream src = await res.Content.ReadAsStreamAsync(ct);
				await using (FileStream dst = File.Create(destPath))
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
				}
				return;
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception ex2)
			{
				last = ex2;
				try
				{
					File.Delete(destPath);
				}
				catch
				{
				}
			}
		}
		throw last ?? new HttpRequestException("Could not download " + url + " from GitHub or any mirror.");
	}
}
