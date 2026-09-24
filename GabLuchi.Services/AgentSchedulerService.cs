using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace GabLuchi.Services;

public static class AgentSchedulerService
{
	public const string TaskName = "GabLuchiAgent";

	public static bool IsAgentRunning()
	{
		try
		{
			using Mutex? m = Mutex.OpenExisting(AgentRunner.AgentMutexName);
			return m != null;
		}
		catch (AbandonedMutexException)
		{
			return true;
		}
		catch (WaitHandleCannotBeOpenedException)
		{
			return false;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public static bool EnsureAgentRunning()
	{
		try
		{
			if (IsAgentRunning())
			{
				return true;
			}
			string? exePath = Environment.ProcessPath;
			if (string.IsNullOrWhiteSpace(exePath))
			{
				return false;
			}
			Process.Start(new ProcessStartInfo(exePath, "--agent")
			{
				UseShellExecute = false,
				CreateNoWindow = true
			});
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public static void Register()
	{
		try
		{
			string? exePath = Environment.ProcessPath;
			if (string.IsNullOrWhiteSpace(exePath))
			{
				return;
			}
			string xml = BuildTaskXml(exePath);
			string tmpRoot = Path.Combine(Path.GetTempPath(), "GabLuchi");
			Directory.CreateDirectory(tmpRoot);
			string tmpFile = Path.Combine(tmpRoot, "GabLuchiAgent-task.xml");
			File.WriteAllText(tmpFile, xml);
			var psi = new ProcessStartInfo
			{
				FileName = "schtasks.exe",
				Arguments = "/Create /TN \"" + TaskName + "\" /XML \"" + tmpFile + "\" /F",
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};
			using Process? p = Process.Start(psi);
			p?.WaitForExit(15000);
			try
			{
				File.Delete(tmpFile);
			}
			catch
			{
			}
		}
		catch (Exception)
		{
		}
	}

	private static string BuildTaskXml(string exePath)
	{
		string escaped = exePath.Replace("&", "&amp;");
		return "<?xml version=\"1.0\" encoding=\"UTF-16\"?>\r\n" +
			"<Task version=\"1.2\" xmlns=\"http://schemas.microsoft.com/windows/2004/02/mit/task\">\r\n" +
			"  <RegistrationInfo>\r\n" +
			"    <Description>Keeps the GabLuchi agent running to persist demolish enforcement.</Description>\r\n" +
			"  </RegistrationInfo>\r\n" +
			"  <Triggers>\r\n" +
			"    <LogonTrigger>\r\n" +
			"      <Enabled>true</Enabled>\r\n" +
			"    </LogonTrigger>\r\n" +
			"    <TimeTrigger>\r\n" +
			"      <StartBoundary>2020-01-01T00:00:00</StartBoundary>\r\n" +
			"      <Enabled>true</Enabled>\r\n" +
			"      <Repetition>\r\n" +
			"        <Interval>PT5M</Interval>\r\n" +
			"        <StopAtDurationEnd>false</StopAtDurationEnd>\r\n" +
			"      </Repetition>\r\n" +
			"    </TimeTrigger>\r\n" +
			"  </Triggers>\r\n" +
			"  <Principals>\r\n" +
			"    <Principal id=\"Author\">\r\n" +
			"      <LogonType>InteractiveToken</LogonType>\r\n" +
			"      <RunLevel>HighestAvailable</RunLevel>\r\n" +
			"    </Principal>\r\n" +
			"  </Principals>\r\n" +
			"  <Settings>\r\n" +
			"    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>\r\n" +
			"    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>\r\n" +
			"    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>\r\n" +
			"    <AllowHardTerminate>true</AllowHardTerminate>\r\n" +
			"    <StartWhenAvailable>true</StartWhenAvailable>\r\n" +
			"    <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>\r\n" +
			"    <IdleSettings>\r\n" +
			"      <StopOnIdleEnd>false</StopOnIdleEnd>\r\n" +
			"      <RestartOnIdle>false</RestartOnIdle>\r\n" +
			"    </IdleSettings>\r\n" +
			"    <AllowStartOnDemand>true</AllowStartOnDemand>\r\n" +
			"    <Enabled>true</Enabled>\r\n" +
			"    <Hidden>false</Hidden>\r\n" +
			"    <RunOnlyIfIdle>false</RunOnlyIfIdle>\r\n" +
			"    <WakeToRun>false</WakeToRun>\r\n" +
			"    <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>\r\n" +
			"    <Priority>7</Priority>\r\n" +
			"  </Settings>\r\n" +
			"  <Actions Context=\"Author\">\r\n" +
			"    <Exec>\r\n" +
			"      <Command>\"" + escaped + "\"</Command>\r\n" +
			"      <Arguments>--agent</Arguments>\r\n" +
			"    </Exec>\r\n" +
			"  </Actions>\r\n" +
			"</Task>\r\n";
	}

	public static void Unregister()
	{
		try
		{
			var psi = new ProcessStartInfo
			{
				FileName = "schtasks.exe",
				Arguments = "/Delete /TN \"" + TaskName + "\" /F",
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