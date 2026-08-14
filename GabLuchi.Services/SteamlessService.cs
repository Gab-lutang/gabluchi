using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GabLuchi.Models;

namespace GabLuchi.Services;

public class SteamlessService(GithubProxy gh, SteamLibraryService library, SteamDepotInfo depots)
{
	private static readonly string ToolDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GabLuchi", "steamless");

	private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true
	};

	private static readonly string[] SkipPatterns = new string[22]
	{
		"redist", "vcredist", "directx", "dxsetup", "vc_redist", "dotnet", "setup", "install", "uninstall", "crashreport",
		"crashhandler", "bugsplat", "easyanticheat", "battleye", "be_service", "launch", "_launcher", "bootstrap", "cefprocess", "cef_process",
		"steamwebhelper", "uplay"
	};

	private readonly SemaphoreSlim _toolGate = new SemaphoreSlim(1, 1);

	private static string CliPath => Path.Combine(ToolDir, "Steamless.CLI.exe");

	public async Task<string?> EnsureToolAsync(IProgress<double?>? progress, CancellationToken ct = default(CancellationToken))
	{
		if (File.Exists(CliPath))
		{
			return CliPath;
		}
		await _toolGate.WaitAsync(ct);
		try
		{
			if (File.Exists(CliPath))
			{
				return CliPath;
			}
			string url = "https://api.github.com/repos/atom0s/Steamless/releases/latest";
			using HttpResponseMessage res = await gh.SendAsync(url, ct);
			if (res == null || !res.IsSuccessStatusCode)
			{
				return null;
			}
			GithubAsset githubAsset = JsonSerializer.Deserialize<GithubRelease>(await res.Content.ReadAsStringAsync(ct), JsonOpts)?.Assets.FirstOrDefault((GithubAsset a) => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
			if (githubAsset == null)
			{
				return null;
			}
			Directory.CreateDirectory(ToolDir);
			string zipPath = Path.Combine(ToolDir, "steamless.zip");
			await gh.DownloadAsync(githubAsset.DownloadUrl, zipPath, progress, ct);
			ZipFile.ExtractToDirectory(zipPath, ToolDir, overwriteFiles: true);
			try
			{
				File.Delete(zipPath);
			}
			catch
			{
			}
			return File.Exists(CliPath) ? CliPath : null;
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
			_toolGate.Release();
		}
	}

	public async Task<SteamlessResult> PatchGameAsync(long appId, IProgress<double?>? progress, CancellationToken ct = default(CancellationToken))
	{
		string installDir = library.GetInstallDir(appId);
		if (installDir == null)
		{
			return new SteamlessResult(0, 0, 0, "no-install");
		}
		string cli = await EnsureToolAsync(progress, ct);
		if (cli == null)
		{
			return new SteamlessResult(0, 0, 0, "tool");
		}
		IReadOnlyList<string> exes = await ResolveExesAsync(appId, installDir, ct);
		if (exes.Count == 0)
		{
			return new SteamlessResult(0, 0, 0, "no-exe");
		}
		int patched = 0;
		int unchanged = 0;
		foreach (string exe in exes)
		{
			ct.ThrowIfCancellationRequested();
			try
			{
				await RunCliAsync(cli, exe, ct);
				string text = exe + ".unpacked.exe";
				if (File.Exists(text))
				{
					string text2 = exe + ".bak";
					if (!File.Exists(text2))
					{
						File.Copy(exe, text2);
					}
					File.Delete(exe);
					File.Move(text, exe);
					patched++;
				}
				else
				{
					unchanged++;
				}
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch
			{
				unchanged++;
				try
				{
					File.Delete(exe + ".unpacked.exe");
				}
				catch
				{
				}
			}
		}
		return new SteamlessResult(patched, unchanged, exes.Count, null);
	}

	private async Task<IReadOnlyList<string>> ResolveExesAsync(long appId, string installDir, CancellationToken ct)
	{
		AppDepotInfo appDepotInfo = await depots.GetAsync(appId, ct);
		if ((object)appDepotInfo != null)
		{
			IReadOnlyList<string> launchExes = appDepotInfo.LaunchExes;
			if (launchExes != null && launchExes.Count > 0)
			{
				List<string> list = appDepotInfo.LaunchExes.Select((string rel) => Path.Combine(installDir, rel)).Where(File.Exists).Distinct<string>(StringComparer.OrdinalIgnoreCase)
					.ToList();
				if (list.Count > 0)
				{
					return list;
				}
			}
		}
		try
		{
			return (from p in Directory.EnumerateFiles(installDir, "*.exe", SearchOption.AllDirectories)
				where !SkipPatterns.Any((string s) => Path.GetFileName(p).Contains(s, StringComparison.OrdinalIgnoreCase))
				select p).Distinct<string>(StringComparer.OrdinalIgnoreCase).ToList();
		}
		catch
		{
			return Array.Empty<string>();
		}
	}

	private static async Task RunCliAsync(string cliPath, string exePath, CancellationToken ct)
	{
		ProcessStartInfo startInfo = new ProcessStartInfo(cliPath, "\"" + exePath + "\"")
		{
			UseShellExecute = false,
			CreateNoWindow = true,
			WorkingDirectory = (Path.GetDirectoryName(cliPath) ?? Environment.CurrentDirectory)
		};
		using Process proc = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start Steamless.");
		await proc.WaitForExitAsync(ct);
	}
}
