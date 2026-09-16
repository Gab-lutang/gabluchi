using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GabLuchi.Models;

namespace GabLuchi.Services;

public class GitHubFixService
{
	private static readonly HttpClient Http = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(30),
		DefaultRequestHeaders =
		{
			{ "User-Agent", "GabLuchi/1.0" }
		}
	};

	private const string IndexUrl =
		"https://raw.githubusercontent.com/Gab-lutang/gabluchi-fixes/main/index.json";

	private const string FixesBaseUrl =
		"https://raw.githubusercontent.com/Gab-lutang/gabluchi-fixes/main/fixes/";

	private static readonly string TempDir = Path.Combine(Path.GetTempPath(), "GabLuchi", "githubfix");

	private static string SevenZipPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7za.exe");

	private List<GitHubFixEntry>? _cachedIndex;

	private DateTime _cacheTime = DateTime.MinValue;

	private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

	public async Task<List<GitHubFixEntry>> FetchIndexAsync(CancellationToken ct = default)
	{
		if (_cachedIndex != null && DateTime.UtcNow - _cacheTime < CacheDuration)
		{
			return _cachedIndex;
		}
		try
		{
			string json = await Http.GetStringAsync(IndexUrl, ct);
			List<GitHubFixEntry>? entries = JsonSerializer.Deserialize<List<GitHubFixEntry>>(json);
			if (entries != null)
			{
				_cachedIndex = entries;
				_cacheTime = DateTime.UtcNow;
				return entries;
			}
		}
		catch
		{
		}
		return _cachedIndex ?? new List<GitHubFixEntry>();
	}

	public async Task<List<GitHubFixEntry>> SearchAsync(string query, CancellationToken ct = default)
	{
		List<GitHubFixEntry> all = await FetchIndexAsync(ct);
		if (string.IsNullOrWhiteSpace(query))
		{
			return all;
		}
		string q = query.Trim().ToLowerInvariant();
		return all.Where(f =>
			f.GameName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
			f.AppId.ToString() == q
		).ToList();
	}

	public async Task<List<GitHubFixEntry>> SearchByAppIdAsync(long appId, CancellationToken ct = default)
	{
		List<GitHubFixEntry> all = await FetchIndexAsync(ct);
		return all.Where(f => f.AppId == appId).ToList();
	}

	public async Task<bool> DownloadFixAsync(GitHubFixEntry entry, string destPath, IProgress<double?>? progress, CancellationToken ct = default)
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
			HttpResponseMessage res = await Http.GetAsync(entry.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
			res.EnsureSuccessStatusCode();
			long? total = res.Content.Headers.ContentLength;
			await using (Stream src = await res.Content.ReadAsStreamAsync(ct))
			{
				await using (FileStream dst = File.Create(destPath))
				{
					byte[] buffer = new byte[81920];
					long written = 0;
					while (true)
					{
						int read = await src.ReadAsync(buffer, ct);
						if (read <= 0) break;
						await dst.WriteAsync(buffer.AsMemory(0, read), ct);
						written += read;
						if (total.HasValue && total.Value > 0)
							progress?.Report((double)written / total.Value);
						else
							progress?.Report(null);
					}
				}
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	public bool ExtractArchive(string archivePath, string outputDir, string password)
	{
		if (!File.Exists(SevenZipPath)) return false;
		Directory.CreateDirectory(outputDir);
		ProcessStartInfo psi = new ProcessStartInfo(SevenZipPath,
			"x \"" + archivePath + "\" -p\"" + password + "\" -o\"" + outputDir + "\" -y")
		{
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};
		using Process? process = Process.Start(psi);
		if (process == null) return false;
		process.WaitForExit();
		return process.ExitCode == 0;
	}

	public async Task<OnlineFixApplyResult> ApplyFixAsync(GitHubFixEntry entry, string gameDir, IProgress<double?>? progress, CancellationToken ct = default)
	{
		if (!File.Exists(SevenZipPath))
		{
			return new OnlineFixApplyResult(false, 0, "7za.exe not found", gameDir);
		}
		try
		{
			Directory.CreateDirectory(TempDir);
			string archivePath = Path.Combine(TempDir, entry.FileName);
			progress?.Report(null);
			bool downloaded = await DownloadFixAsync(entry, archivePath, progress, ct);
			if (!downloaded)
			{
				return new OnlineFixApplyResult(false, 0, "Failed to download from GitHub", gameDir);
			}
			string extractDir = Path.Combine(TempDir, "extract_" + entry.AppId);
			if (Directory.Exists(extractDir))
			{
				Directory.Delete(extractDir, recursive: true);
			}
			bool extracted = ExtractArchive(archivePath, extractDir, entry.Password);
			if (!extracted)
			{
				return new OnlineFixApplyResult(false, 0, "Failed to extract archive", gameDir);
			}
			int filesInstalled = ApplyToGame(extractDir, gameDir, entry.AppId);
			return new OnlineFixApplyResult(true, filesInstalled, null, gameDir);
		}
		catch (Exception ex)
		{
			return new OnlineFixApplyResult(false, 0, ex.Message, gameDir);
		}
	}

	private static int ApplyToGame(string extractedDir, string gameDir, long expectedAppId)
	{
		int copied = 0;
		string[] files = Directory.GetFiles(extractedDir, "*", SearchOption.AllDirectories);
		foreach (string file in files)
		{
			string relativePath = file[(extractedDir.Length + 1)..];
			string lower = Path.GetFileName(relativePath).ToLowerInvariant();
			if (!IsFixFile(lower)) continue;
			string dest = Path.Combine(gameDir, relativePath);
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
				if (File.Exists(dest))
				{
					string bak = dest + ".bak";
					if (!File.Exists(bak))
					{
						File.Copy(dest, bak, overwrite: false);
					}
				}
				File.Copy(file, dest, overwrite: true);
				copied++;
			}
			catch
			{
			}
		}
		if (copied > 0)
		{
			FixOnlineFixIni(extractedDir, gameDir, expectedAppId);
		}
		return copied;
	}

	private static void FixOnlineFixIni(string extractedDir, string gameDir, long appId)
	{
		string iniSource = Path.Combine(extractedDir, "OnlineFix.ini");
		if (!File.Exists(iniSource)) return;
		string iniDest = Path.Combine(gameDir, "OnlineFix.ini");
		try
		{
			string content = File.ReadAllText(iniSource);
			if (!content.Contains("RealAppId") || !content.Contains(appId.ToString()))
			{
				content = content.Replace("RealAppId=", "RealAppId=" + appId);
			}
			File.WriteAllText(iniDest, content);
		}
		catch
		{
			try { File.Copy(iniSource, iniDest, overwrite: true); } catch { }
		}
	}

	private static bool IsFixFile(string lowerFileName)
	{
		return lowerFileName.EndsWith(".dll") ||
			lowerFileName.EndsWith(".ini") ||
			lowerFileName.EndsWith(".txt") ||
			lowerFileName == "onlinefix64.dll" ||
			lowerFileName == "onlinefix.dll" ||
			lowerFileName == "winmm.dll" ||
			lowerFileName == "winhttp.dll" ||
			lowerFileName == "dnet.dll" ||
			lowerFileName == "steamoverlay64.dll" ||
			lowerFileName == "steam_api64.dll" ||
			lowerFileName == "steam_api.dll" ||
			lowerFileName == "onlinefix.ini";
	}
}
