using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using GabLuchi.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GabLuchi.Services;

public class BackgroundHealthScanner : IHostedService
{
	private readonly GameHealthService _health;
	private readonly DefenderService _defender;
	private readonly SteamLibraryService _library;
	private readonly ILogger<BackgroundHealthScanner> _log;
	private CancellationTokenSource? _cts;

	private const int InitialDelaySeconds = 60;
	private const int ScanIntervalSeconds = 1800;

	private volatile bool _scanning;
	private DateTime _lastScanTime = DateTime.MinValue;
	private int _totalIssuesFound;
	private int _gamesWithIssues;
	private int _quarantinedDlls;

	public bool IsScanning => _scanning;
	public DateTime LastScanTime => _lastScanTime;
	public int TotalIssuesFound => _totalIssuesFound;
	public int GamesWithIssues => _gamesWithIssues;
	public int QuarantinedDlls => _quarantinedDlls;
	public IReadOnlyList<GameHealthReport> LastResults => _lastResults;

	private List<GameHealthReport> _lastResults = new();

	public BackgroundHealthScanner(
		GameHealthService health,
		DefenderService defender,
		SteamLibraryService library,
		ILogger<BackgroundHealthScanner> log)
	{
		_health = health;
		_defender = defender;
		_library = library;
		_log = log;
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		_ = ScanLoopAsync(_cts.Token);
		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		_cts?.Cancel();
		return Task.CompletedTask;
	}

	private async Task ScanLoopAsync(CancellationToken ct)
	{
		await Task.Delay(InitialDelaySeconds * 1000, ct).ConfigureAwait(false);

		while (!ct.IsCancellationRequested)
		{
			try
			{
				await RunScanAsync(ct).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch (Exception ex)
			{
				_log.LogWarning("Background health scan failed: {Message}", ex.Message);
			}

			await Task.Delay(ScanIntervalSeconds * 1000, ct).ConfigureAwait(false);
		}
	}

	public async Task RunScanAsync(CancellationToken ct = default)
	{
		if (_scanning)
		{
			return;
		}

		_scanning = true;
		try
		{
			_log.LogInformation("Background health scan starting...");

			int quarantined = await _defender.GetQuarantinedGabLuchiCountAsync();
			Interlocked.Exchange(ref _quarantinedDlls, quarantined);

			if (quarantined > 0)
			{
				_log.LogWarning("AV quarantine detected: {Count} GabLuchi DLLs quarantined", quarantined);
			}

			List<long> appIds = _library.GetInstalledAppIds();
			List<GameHealthReport> reports = new();
			int issuesFound = 0;
			int gamesIssues = 0;

			foreach (long appId in appIds)
			{
				if (ct.IsCancellationRequested)
					break;

				try
				{
					GameHealthReport report = await _health.ScanSingleAsync(appId);
					reports.Add(report);

					if (report.Issues.Count > 0)
					{
						gamesIssues++;
						issuesFound += report.Issues.Count;
					}
				}
				catch
				{
				}

				await Task.Delay(10, ct).ConfigureAwait(false);
			}

			_lastResults = reports;
			Interlocked.Exchange(ref _totalIssuesFound, issuesFound);
			Interlocked.Exchange(ref _gamesWithIssues, gamesIssues);
			_lastScanTime = DateTime.Now;

			_log.LogInformation("Background health scan complete: {Games} games, {Issues} issues in {GamesWithIssues} games, {Quarantined} quarantined DLLs",
				reports.Count, issuesFound, gamesIssues, quarantined);
		}
		finally
		{
			_scanning = false;
		}
	}
}
