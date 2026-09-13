using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GabLuchi.Models;

namespace GabLuchi.Services;

public class OnlineFixService(SteamLibraryService library, ToastService toast)
{
	private static readonly HttpClient Http = new HttpClient
	{
		Timeout = TimeSpan.FromMinutes(10.0),
		DefaultRequestHeaders =
		{
			{ "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36" }
		}
	};

	private const string PerondepotIndex = "http://api.perondepot.xyz/all/";

	private const string ArchivePassword = "online-fix.me";

	private static readonly Regex LinkRegex = new Regex("<a[^>]+href=\"([^\"]+\\.rar)\"[^>]*>([^<]+)</a>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex SizeRegex = new Regex("(\\d+)", RegexOptions.Compiled);

	private static readonly string TempDir = Path.Combine(Path.GetTempPath(), "GabLuchi", "onlinefix");

	private static string SevenZipPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7za.exe");

	public bool Is7ZipAvailable => File.Exists(SevenZipPath);

	public async Task<List<OnlineFixEntry>> SearchAsync(string query, CancellationToken ct = default)
	{
		HttpResponseMessage res = await Http.GetAsync(PerondepotIndex, ct);
		res.EnsureSuccessStatusCode();
		byte[] bytes = await res.Content.ReadAsByteArrayAsync(ct);
		string html = Encoding.Latin1.GetString(bytes);
		return ParseIndex(html, query);
	}

	public async Task<List<OnlineFixEntry>> SearchByAppIdAsync(long appId, CancellationToken ct = default)
	{
		HttpResponseMessage res = await Http.GetAsync(PerondepotIndex, ct);
		res.EnsureSuccessStatusCode();
		byte[] bytes = await res.Content.ReadAsByteArrayAsync(ct);
		string html = Encoding.Latin1.GetString(bytes);
		string prefix = "[" + appId + "]";
		return ParseIndex(html, prefix);
	}

	private static List<OnlineFixEntry> ParseIndex(string html, string query)
	{
		List<OnlineFixEntry> results = new List<OnlineFixEntry>();
		MatchCollection matches = LinkRegex.Matches(html);
		string normalisedQuery = query.Trim().ToLowerInvariant();
		foreach (Match match in matches)
		{
			string href = match.Groups[1].Value;
			string displayText = match.Groups[2].Value.Trim();
			if (string.IsNullOrEmpty(href) || string.IsNullOrEmpty(displayText))
			{
				continue;
			}
			if (!displayText.EndsWith(".rar", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			string fileName = displayText;
			string gameName = ExtractGameName(fileName);
			long appId = ExtractAppId(fileName);
			if (appId <= 0)
			{
				continue;
			}
			if (!string.IsNullOrEmpty(normalisedQuery) &&
				!gameName.ToLowerInvariant().Contains(normalisedQuery) &&
				!fileName.ToLowerInvariant().Contains(normalisedQuery) &&
				!("[" + appId + "]").Contains(normalisedQuery))
			{
				continue;
			}
			string downloadUrl = PerondepotIndex + Uri.EscapeDataString(fileName);
			results.Add(new OnlineFixEntry(appId, gameName, fileName, 0L, downloadUrl));
		}
		return results.DistinctBy(e => e.AppId).ToList();
	}

	private static string ExtractGameName(string fileName)
	{
		string name = Path.GetFileNameWithoutExtension(fileName);
		int bracketEnd = name.IndexOf(']');
		if (bracketEnd >= 0 && bracketEnd + 1 < name.Length)
		{
			name = name[(bracketEnd + 1)..].TrimStart('_').Replace('_', ' ');
		}
		return name;
	}

	private static long ExtractAppId(string fileName)
	{
		Match m = SizeRegex.Match(fileName);
		if (m.Success && long.TryParse(m.Groups[1].Value, out long id))
		{
			return id;
		}
		return 0;
	}

	public async Task<string> DownloadAsync(OnlineFixEntry entry, IProgress<double?>? progress, CancellationToken ct = default)
	{
		Directory.CreateDirectory(TempDir);
		string filePath = Path.Combine(TempDir, entry.FileName);
		HttpResponseMessage res = await Http.GetAsync(entry.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
		res.EnsureSuccessStatusCode();
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
					if (total.HasValue && total.Value > 0)
					{
						progress?.Report((double)written / total.Value);
					}
					else
					{
						progress?.Report(null);
					}
				}
			}
		}
		return filePath;
	}

	public bool ExtractArchive(string archivePath, string outputDir)
	{
		if (!File.Exists(SevenZipPath))
		{
			return false;
		}
		Directory.CreateDirectory(outputDir);
		ProcessStartInfo psi = new ProcessStartInfo(SevenZipPath, "x \"" + archivePath + "\" -p\"" + ArchivePassword + "\" -o\"" + outputDir + "\" -y")
		{
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};
		using Process? process = Process.Start(psi);
		if (process == null)
		{
			return false;
		}
		process.WaitForExit();
		return process.ExitCode == 0;
	}

	public int ApplyToGame(string extractedDir, string gameDir, long expectedAppId)
	{
		int copied = 0;
		string[] files = Directory.GetFiles(extractedDir, "*", SearchOption.AllDirectories);
		foreach (string file in files)
		{
			string relativePath = file[(extractedDir.Length + 1)..];
			string fileName = Path.GetFileName(relativePath);
			string lower = fileName.ToLowerInvariant();
			if (!IsFixFile(lower))
			{
				continue;
			}
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
		if (!File.Exists(iniSource))
		{
			return;
		}
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
			try
			{
				File.Copy(iniSource, iniDest, overwrite: true);
			}
			catch
			{
			}
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

	public string? GetGameDir(long appId)
	{
		return library.GetInstallDir(appId);
	}

	public async Task<OnlineFixApplyResult> ApplyFixAsync(OnlineFixEntry entry, string gameDir, IProgress<double?>? progress, CancellationToken ct = default)
	{
		if (!File.Exists(SevenZipPath))
		{
			return new OnlineFixApplyResult(false, 0, "7za.exe not found", gameDir);
		}
		try
		{
			progress?.Report(null);
			string archivePath = await DownloadAsync(entry, progress, ct);
			string extractDir = Path.Combine(TempDir, "extract_" + entry.AppId);
			if (Directory.Exists(extractDir))
			{
				Directory.Delete(extractDir, recursive: true);
			}
			bool extracted = ExtractArchive(archivePath, extractDir);
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
}
