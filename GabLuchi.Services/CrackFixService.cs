using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GabLuchi.Models;

namespace GabLuchi.Services;

public class CrackFixService(SteamLibraryService library, ToastService toast)
{
	private static readonly HttpClient Http = new HttpClient
	{
		Timeout = TimeSpan.FromMinutes(5.0),
		DefaultRequestHeaders =
		{
			{ "User-Agent", "GabLuchi/1.0" }
		}
	};

	private static readonly string CacheDir = Path.Combine(Path.GetTempPath(), "GabLuchi");

	private static readonly string CacheFile = Path.Combine(CacheDir, "crackfix_cache.json");

	private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24.0);

	private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true
	};

	private static readonly Regex PixeldrainIdRegex = new Regex("pixeldrain\\.com/u/([a-zA-Z0-9]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex BuzzheavierRegex = new Regex("buzzheavier\\.com/([a-zA-Z0-9]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private const string PixeldrainApiBase = "https://pixeldrain.com/api/file/";

	private const string CrackFilesJsonUrl = "https://raw.githubusercontent.com/KoriaPolis/CrakFiles/main/crackfiles.json";

	private const string ZipPassword = "cs.rin.ru";

	private static string SevenZipPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7za.exe");

	public bool Is7ZipAvailable => File.Exists(SevenZipPath);

	public async Task<List<CrackFixEntry>> FetchFixesAsync(CancellationToken ct = default)
	{
		if (File.Exists(CacheFile))
		{
			try
			{
				DateTime lastWrite = File.GetLastWriteTimeUtc(CacheFile);
				if (DateTime.UtcNow - lastWrite < CacheTtl)
				{
					string cached = await File.ReadAllTextAsync(CacheFile, ct);
					List<CrackFixEntry>? cachedEntries = JsonSerializer.Deserialize<List<CrackFixEntry>>(cached, JsonOpts);
					if (cachedEntries != null && cachedEntries.Count > 0)
					{
						return cachedEntries;
					}
				}
			}
			catch
			{
			}
		}
		HttpResponseMessage res = await Http.GetAsync(CrackFilesJsonUrl, ct);
		res.EnsureSuccessStatusCode();
		string json = await res.Content.ReadAsStringAsync(ct);
		List<CrackFixEntry>? entries = JsonSerializer.Deserialize<List<CrackFixEntry>>(json, JsonOpts);
		try
		{
			Directory.CreateDirectory(CacheDir);
			await File.WriteAllTextAsync(CacheFile, json, ct);
		}
		catch
		{
		}
		return entries ?? new List<CrackFixEntry>();
	}

	public List<CrackFixEntry> Search(string query, List<CrackFixEntry> allFixes)
	{
		if (string.IsNullOrWhiteSpace(query))
		{
			return allFixes;
		}
		string normalised = query.Trim().ToLowerInvariant();
		List<CrackFixEntry> exact = allFixes
			.Where(e => e.Name.Equals(normalised, StringComparison.OrdinalIgnoreCase))
			.ToList();
		if (exact.Count > 0)
		{
			return exact;
		}
		List<CrackFixEntry> contains = allFixes
			.Where(e => e.Name.Contains(normalised, StringComparison.OrdinalIgnoreCase))
			.ToList();
		if (contains.Count > 0)
		{
			return contains;
		}
		string[] tokens = normalised.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		return allFixes
			.Where(e => tokens.All(t => e.Name.Contains(t, StringComparison.OrdinalIgnoreCase)))
			.OrderByDescending(e => CountMatches(e.Name, tokens))
			.ToList();
	}

	private static int CountMatches(string name, string[] tokens)
	{
		int count = 0;
		string lower = name.ToLowerInvariant();
		foreach (string token in tokens)
		{
			if (lower.Contains(token))
			{
				count++;
			}
		}
		return count;
	}

	public async Task<string> DownloadFixAsync(CrackFixItem fix, string tempDir, IProgress<double?>? progress, CancellationToken ct = default)
	{
		string? fileId = ExtractPixeldrainId(fix.Href);
		if (fileId == null)
		{
			throw new NotSupportedException("Only pixeldrain URLs are currently supported.");
		}
		string downloadUrl = PixeldrainApiBase + fileId;
		string filePath = Path.Combine(tempDir, fix.Filename);
		HttpResponseMessage res = await Http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
		if (res.StatusCode == System.Net.HttpStatusCode.Forbidden)
		{
			string pageUrl = "https://pixeldrain.com/u/" + fileId;
			System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(pageUrl) { UseShellExecute = true });
			throw new NotSupportedException($"Download blocked by pixeldrain (captcha required). Opened in browser: {pageUrl}");
		}
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
		ProcessStartInfo psi = new ProcessStartInfo(SevenZipPath, "x \"" + archivePath + "\" -p\"" + ZipPassword + "\" -o\"" + outputDir + "\" -y")
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

	public int ApplyToGame(string extractedDir, string gameDir)
	{
		int copied = 0;
		int failed = 0;
		string[] files = Directory.GetFiles(extractedDir, "*", SearchOption.AllDirectories);
		foreach (string file in files)
		{
			string relativePath = file[(extractedDir.Length + 1)..];
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
				failed++;
			}
		}
		return copied;
	}

	public string? GetGameDir(long appId)
	{
		return library.GetInstallDir(appId);
	}

	private static string? ExtractPixeldrainId(string url)
	{
		Match m = PixeldrainIdRegex.Match(url);
		if (m.Success)
		{
			return m.Groups[1].Value;
		}
		Match m2 = BuzzheavierRegex.Match(url);
		if (m2.Success)
		{
			return m2.Groups[1].Value;
		}
		return null;
	}
}
