using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GabLuchi.Services;

public class CompanionService : IHostedService
{
	private sealed class Companion
	{
		public string Script { get; init; } = "";

		public int Port { get; init; }

		public Process? Process { get; set; }
	}

	private readonly ILogger<CompanionService> _log;

	private readonly List<Companion> _companions = new List<Companion>
	{
		new Companion { Script = "server.js", Port = 4567 },
		new Companion { Script = "bot.js", Port = 7890 }
	};

	public CompanionService(ILogger<CompanionService> logger)
	{
		_log = logger;
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		foreach (Companion companion in _companions)
		{
			try
			{
				string? scriptPath = ResolveScript(companion.Script);
				if (scriptPath == null)
				{
					_log.LogDebug("Companion script {Script} not found — skipping.", companion.Script);
					continue;
				}
				if (!IsPortFree(companion.Port))
				{
					_log.LogInformation("Port {Port} already in use — {Script} assumed running, not starting another.", companion.Port, companion.Script);
					continue;
				}
				companion.Process = Process.Start(new ProcessStartInfo("node", "\"" + scriptPath + "\"")
				{
					WorkingDirectory = Path.GetDirectoryName(scriptPath),
					UseShellExecute = false,
					CreateNoWindow = true,
					WindowStyle = ProcessWindowStyle.Hidden
				});
				if (companion.Process != null)
				{
					_log.LogInformation("{Script} started (PID {Pid}).", companion.Script, companion.Process.Id);
				}
			}
			catch (Exception ex)
			{
				_log.LogDebug("Could not auto-start {Script}: {Message}", companion.Script, ex.Message);
			}
		}
		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		foreach (Companion companion in _companions)
		{
			try
			{
				if (companion.Process != null && !companion.Process.HasExited)
				{
					_log.LogInformation("Stopping {Script} (PID {Pid}).", companion.Script, companion.Process.Id);
					companion.Process.Kill(entireProcessTree: true);
					companion.Process.WaitForExit(2000);
				}
			}
			catch
			{
			}
			companion.Process?.Dispose();
			companion.Process = null;
		}
		return Task.CompletedTask;
	}

	private static string? ResolveScript(string script)
	{
		string configured = Config.BackendDir;
		if (!string.IsNullOrWhiteSpace(configured))
		{
			string candidate = Path.Combine(configured, script);
			if (File.Exists(candidate))
			{
				return candidate;
			}
		}
		DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
		while (dir != null)
		{
			string candidate = Path.Combine(dir.FullName, "backend", script);
			if (File.Exists(candidate))
			{
				return candidate;
			}
			dir = dir.Parent;
		}
		return null;
	}

	private static bool IsPortFree(int port)
	{
		try
		{
			using TcpListener listener = new TcpListener(IPAddress.Loopback, port);
			listener.Start();
			listener.Stop();
			return true;
		}
		catch
		{
			return false;
		}
	}
}
