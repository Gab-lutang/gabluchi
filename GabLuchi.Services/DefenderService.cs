using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace GabLuchi.Services;

public class DefenderService
{
	private static readonly string[] GabLuchiDllNames = new[]
	{
		"winmm.dll", "winmm_real.dll", "OnlineFix64.dll", "OnlineFix.dll"
	};

	public async Task HardenAsync()
	{
		string steamRoot = @"C:\Program Files (x86)\Steam";
		string appRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\GabLuchi";
		await RunPsAsync("Add-MpPreference -ExclusionPath '" + steamRoot + "'");
		await RunPsAsync("Add-MpPreference -ExclusionPath '" + appRoot + "'");
		await RunPsAsync("Set-MpPreference -DisableRealtimeMonitoring $true");
	}

	public async Task<int> GetQuarantinedGabLuchiCountAsync()
	{
		try
		{
			string result = await RunPsReturnAsync(
				"Get-MpThreatDetection | Where-Object { $_.Resources -match 'GabLuchi|winmm|OnlineFix' } | Measure-Object | Select-Object -ExpandProperty Count");
			if (int.TryParse(result.Trim(), out int count))
			{
				return count;
			}
		}
		catch
		{
		}
		return 0;
	}

	public async Task<DefenderStatus> GetStatusAsync()
	{
		bool rtmEnabled = false;
		bool exclusionsSet = false;
		int quarantinedCount = 0;

		try
		{
			string rtm = await RunPsReturnAsync(
				"(Get-MpPreference).DisableRealtimeMonitoring");
			rtmEnabled = rtm.Trim().Equals("False", StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
		}

		try
		{
			string appRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\GabLuchi";
			string exclusions = await RunPsReturnAsync(
				"(Get-MpPreference).ExclusionPath");
			exclusionsSet = exclusions.Contains(appRoot, StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
		}

		quarantinedCount = await GetQuarantinedGabLuchiCountAsync();

		return new DefenderStatus(rtmEnabled, exclusionsSet, quarantinedCount);
	}

	public async Task<bool> ReExcludeGabLuchiAsync()
	{
		try
		{
			string steamRoot = GetSteamPath();
			string appRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\GabLuchi";
			await RunPsAsync("Add-MpPreference -ExclusionPath '" + steamRoot + "'");
			await RunPsAsync("Add-MpPreference -ExclusionPath '" + appRoot + "'");

			string gameRoot = Path.Combine(steamRoot, "steamapps", "common");
			if (Directory.Exists(gameRoot))
			{
				await RunPsAsync("Add-MpPreference -ExclusionPath '" + gameRoot + "'");
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static string GetSteamPath()
	{
		try
		{
			using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Valve\\Steam");
			if (key?.GetValue("SteamPath") is string path)
			{
				return path.Replace('/', '\\');
			}
		}
		catch
		{
		}
		return @"C:\Program Files (x86)\Steam";
	}

	private static async Task RunPsAsync(string command)
	{
		try
		{
			using Process process = new Process();
			process.StartInfo = new ProcessStartInfo("powershell.exe", "-NoProfile -NonInteractive -WindowStyle Hidden -Command \"" + command.Replace("\"", "\\\"") + "\"")
			{
				UseShellExecute = false,
				CreateNoWindow = true
			};
			process.Start();
			await process.WaitForExitAsync();
		}
		catch
		{
		}
	}

	private static async Task<string> RunPsReturnAsync(string command)
	{
		try
		{
			using Process process = new Process();
			process.StartInfo = new ProcessStartInfo("powershell.exe", "-NoProfile -NonInteractive -WindowStyle Hidden -Command \"" + command.Replace("\"", "\\\"") + "\"")
			{
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true
			};
			process.Start();
			StringBuilder output = new StringBuilder();
			while (!process.StandardOutput.EndOfStream)
			{
				output.AppendLine(await process.StandardOutput.ReadLineAsync());
			}
			await process.WaitForExitAsync();
			return output.ToString();
		}
		catch
		{
			return string.Empty;
		}
	}
}

public record DefenderStatus(bool RealTimeProtectionEnabled, bool ExclusionsSet, int QuarantinedDllCount);
