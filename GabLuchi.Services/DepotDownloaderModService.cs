using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GabLuchi.Models;

namespace GabLuchi.Services;

public partial class DepotDownloaderModService
{
	private static readonly string ToolDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DepotDownloaderMod");

	private static string DllPath => Path.Combine(ToolDir, "DepotDownloaderMod.dll");

	private readonly SemaphoreSlim _dotnetCheckGate = new SemaphoreSlim(1, 1);

	private bool? _isDotNet9Available;

	public bool IsDotNet9Available => _isDotNet9Available ?? false;

	public bool IsToolPresent => File.Exists(DllPath);

	public async Task<bool> CheckDotNet9Async(CancellationToken ct = default)
	{
		if (_isDotNet9Available.HasValue)
		{
			return _isDotNet9Available.Value;
		}
		await _dotnetCheckGate.WaitAsync(ct);
		try
		{
			if (_isDotNet9Available.HasValue)
			{
				return _isDotNet9Available.Value;
			}
			_isDotNet9Available = await DetectDotNet9Async(ct);
			return _isDotNet9Available.Value;
		}
		finally
		{
			_dotnetCheckGate.Release();
		}
	}

	private static async Task<bool> DetectDotNet9Async(CancellationToken ct)
	{
		try
		{
			ProcessStartInfo startInfo = new ProcessStartInfo("dotnet", "--list-runtimes")
			{
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true
			};
			using Process proc = Process.Start(startInfo);
			if (proc == null)
			{
				return false;
			}
			string output = await proc.StandardOutput.ReadToEndAsync(ct);
			await proc.WaitForExitAsync(ct);
			return output.Contains("Microsoft.NETCore.App 9.", StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	public async Task<DepotDownloadResult> DownloadDepotAsync(
		int appId,
		int depotId,
		string manifestId,
		string? depotKey,
		string destDir,
		IProgress<double?>? progress,
		CancellationToken ct = default)
	{
		if (!IsToolPresent)
		{
			return new DepotDownloadResult(false, "DepotDownloaderMod.dll not found in third_party/DepotDownloaderMod/.", 0, destDir);
		}
		bool dotNet9 = await CheckDotNet9Async(ct);
		if (!dotNet9)
		{
			return new DepotDownloadResult(false, ".NET 9 runtime is not installed. Please install it from https://dotnet.microsoft.com/download/dotnet/9.0", 0, destDir);
		}
		Directory.CreateDirectory(destDir);
		string? depotKeysFile = null;
		if (!string.IsNullOrWhiteSpace(depotKey))
		{
			depotKeysFile = Path.Combine(Path.GetTempPath(), "GabLuchi", "depotkeys_" + Guid.NewGuid().ToString("N") + ".txt");
			Directory.CreateDirectory(Path.GetDirectoryName(depotKeysFile)!);
			await File.WriteAllTextAsync(depotKeysFile, $"{depotId};{ depotKey.Trim()}", ct);
		}
		try
		{
			string args = BuildArgs(appId, depotId, manifestId, depotKeysFile, destDir);
			DepotDownloadResult result = await RunDownloadAsync(args, progress, ct);
			return result;
		}
		finally
		{
			if (depotKeysFile != null)
			{
				try { File.Delete(depotKeysFile); } catch { }
			}
		}
	}

	private static string BuildArgs(int appId, int depotId, string manifestId, string? depotKeysFile, string destDir)
	{
		return $"\"{DllPath}\" -app {appId} -depot {depotId} -manifest {manifestId} -dir \"{destDir}\" -os windows -max-downloads 32 -validate"
			+ (depotKeysFile != null ? $" -depotkeys \"{depotKeysFile}\"" : "");
	}

	private static async Task<DepotDownloadResult> RunDownloadAsync(
		string args,
		IProgress<double?>? progress,
		CancellationToken ct)
	{
		int filesDownloaded = 0;
		string? lastError = null;
		try
		{
			ProcessStartInfo startInfo = new ProcessStartInfo("dotnet", args)
			{
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				WorkingDirectory = ToolDir
			};
			using Process proc = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start DepotDownloaderMod.");
			using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
			timeout.CancelAfter(TimeSpan.FromMinutes(10.0));
			proc.OutputDataReceived += (_, e) =>
			{
				if (e.Data == null) return;
				Match match = ProgressRegex().Match(e.Data);
				if (match.Success && double.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double pct))
				{
					progress?.Report(pct / 100.0);
				}
			};
			proc.ErrorDataReceived += (_, e) =>
			{
				if (e.Data != null)
				{
					lastError = e.Data;
				}
			};
			proc.BeginOutputReadLine();
			proc.BeginErrorReadLine();
			try
			{
				await proc.WaitForExitAsync(timeout.Token);
			}
			catch (OperationCanceledException) when (!ct.IsCancellationRequested)
			{
				try { proc.Kill(entireProcessTree: true); } catch { }
				return new DepotDownloadResult(false, "Download timed out after 10 minutes.", 0, "");
			}
			if (proc.ExitCode != 0)
			{
				string errorMsg = proc.ExitCode switch
				{
					-2146233082 => "Outdated DepotDownloaderMod. Please update the bundled DLL.",
					-1073741819 => "Missing .NET 9 runtime DLL. Please reinstall .NET 9.",
					_ => $"DepotDownloaderMod exited with code {proc.ExitCode}. {(lastError ?? "")}"
				};
				return new DepotDownloadResult(false, errorMsg.Trim(), 0, "");
			}
			progress?.Report(1.0);
			return new DepotDownloadResult(true, null, filesDownloaded, "");
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			return new DepotDownloadResult(false, ex.Message, 0, "");
		}
	}

	[GeneratedRegex(@"^\s*(\d{1,3}(?:\.\d+)?)%\s", RegexOptions.Multiline)]
	private static partial Regex ProgressRegex();
}
