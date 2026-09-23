using System;
using System.Diagnostics;

namespace GabLuchi.Services;

public static class AgentSchedulerService
{
	public const string TaskName = "GabLuchiAgent";

	public static void Register()
	{
		try
		{
			string? exePath = Environment.ProcessPath;
			if (string.IsNullOrWhiteSpace(exePath))
			{
				return;
			}
			var psi = new ProcessStartInfo
			{
				FileName = "schtasks.exe",
				Arguments = "/Create /TN \"GabLuchiAgent\" /TR \"\\\"" + exePath + "\\\" --agent\" /SC ONLOGON /RL HIGHEST /F",
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};
			using Process? p = Process.Start(psi);
			p?.WaitForExit(15000);
		}
		catch (Exception)
		{
		}
	}

	public static void Unregister()
	{
		try
		{
			var psi = new ProcessStartInfo
			{
				FileName = "schtasks.exe",
				Arguments = "/Delete /TN \"GabLuchiAgent\" /F",
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};
			using Process? p = Process.Start(psi);
			p?.WaitForExit(15000);
		}
		catch (Exception)
		{
		}
	}
}