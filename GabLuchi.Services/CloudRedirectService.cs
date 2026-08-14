using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GabLuchi.Models;

namespace GabLuchi.Services;

public class CloudRedirectService(GithubProxy gh)
{
	private static readonly string ToolDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GabLuchi", "cloudredirect");

	private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true
	};

	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);

	private static string ExePath => Path.Combine(ToolDir, "CloudRedirect.exe");

	public async Task<string?> EnsureToolAsync(IProgress<double?>? progress, CancellationToken ct = default(CancellationToken))
	{
		if (File.Exists(ExePath))
		{
			return ExePath;
		}
		await _gate.WaitAsync(ct);
		try
		{
			if (File.Exists(ExePath))
			{
				return ExePath;
			}
			string url = "https://api.github.com/repos/Selectively11/CloudRedirect/releases/latest";
			using HttpResponseMessage res = await gh.SendAsync(url, ct);
			if (res == null || !res.IsSuccessStatusCode)
			{
				return null;
			}
			GithubAsset githubAsset = JsonSerializer.Deserialize<GithubRelease>(await res.Content.ReadAsStringAsync(ct), JsonOpts)?.Assets.FirstOrDefault((GithubAsset a) => a.Name.Equals("CloudRedirect.exe", StringComparison.OrdinalIgnoreCase));
			if (githubAsset == null)
			{
				return null;
			}
			Directory.CreateDirectory(ToolDir);
			await gh.DownloadAsync(githubAsset.DownloadUrl, ExePath, progress, ct);
			return File.Exists(ExePath) ? ExePath : null;
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch
		{
			return null;
		}
		finally
		{
			_gate.Release();
		}
	}

	public async Task<bool> LaunchAsync(IProgress<double?>? progress, CancellationToken ct = default(CancellationToken))
	{
		string text = await EnsureToolAsync(progress, ct);
		if (text == null)
		{
			return false;
		}
		try
		{
			Process.Start(new ProcessStartInfo(text)
			{
				UseShellExecute = true,
				WorkingDirectory = ToolDir
			});
			return true;
		}
		catch
		{
			return false;
		}
	}
}
