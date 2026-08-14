using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Velopack.Sources;

namespace GabLuchi.Services;

public class ProxiedFileDownloader : IFileDownloader
{
	private readonly HttpClientFileDownloader _inner = new HttpClientFileDownloader();

	public async Task DownloadFile(string url, string targetFile, Action<int> progress, IDictionary<string, string>? headers = null, double timeout = 30.0, CancellationToken cancelToken = default(CancellationToken))
	{
		Exception ex = null;
		foreach (string item in GithubProxy.Candidates(url))
		{
			try
			{
				await _inner.DownloadFile(item, targetFile, progress, headers, timeout, cancelToken);
				return;
			}
			catch (OperationCanceledException) when (cancelToken.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception ex3)
			{
				ex = ex3;
			}
		}
		throw ex ?? new Exception("Failed to download " + url + " from GitHub or any mirror.");
	}

	public async Task<byte[]> DownloadBytes(string url, IDictionary<string, string>? headers = null, double timeout = 30.0)
	{
		Exception ex = null;
		foreach (string item in GithubProxy.Candidates(url))
		{
			try
			{
				return await _inner.DownloadBytes(item, headers, timeout);
			}
			catch (Exception ex2)
			{
				ex = ex2;
			}
		}
		throw ex ?? new Exception("Failed to download " + url + " from GitHub or any mirror.");
	}

	public async Task<string> DownloadString(string url, IDictionary<string, string>? headers = null, double timeout = 30.0)
	{
		Exception ex = null;
		foreach (string item in GithubProxy.Candidates(url))
		{
			try
			{
				return await _inner.DownloadString(item, headers, timeout);
			}
			catch (Exception ex2)
			{
				ex = ex2;
			}
		}
		throw ex ?? new Exception("Failed to download " + url + " from GitHub or any mirror.");
	}
}
