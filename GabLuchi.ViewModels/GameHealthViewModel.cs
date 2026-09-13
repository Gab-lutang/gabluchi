using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GabLuchi.Models;
using GabLuchi.Resources;
using GabLuchi.Services;

namespace GabLuchi.ViewModels;

public partial class GameHealthViewModel : ObservableObject
{
	private readonly GameHealthService _health;
	private readonly ToastService _toast;
	private readonly SteamAppListCache _appList;

	private ObservableCollection<GameHealthReport> _results = new();
	public ObservableCollection<GameHealthReport> Results
	{
		get => _results;
		set => SetProperty(ref _results, value);
	}

	private bool _isScanning;
	public bool IsScanning
	{
		get => _isScanning;
		set { if (SetProperty(ref _isScanning, value)) OnPropertyChanged(nameof(IsNotScanning)); }
	}

	public bool IsNotScanning => !IsScanning;

	private bool _isFixing;
	public bool IsFixing
	{
		get => _isFixing;
		set => SetProperty(ref _isFixing, value);
	}

	private string _progressText = string.Empty;
	public string ProgressText
	{
		get => _progressText;
		set => SetProperty(ref _progressText, value);
	}

	private string _scanButtonText = Strings.Health_ScanAll;
	public string ScanButtonText
	{
		get => _scanButtonText;
		set => SetProperty(ref _scanButtonText, value);
	}

	public bool HasResults => Results.Count > 0;

	public string SummaryText
	{
		get
		{
			if (Results.Count == 0)
				return string.Empty;
			int healthy = Results.Count(r => r.IsHealthy);
			int unhealthy = Results.Count - healthy;
			return $"{healthy} healthy, {unhealthy} need attention ({Results.Count} total)";
		}
	}

	public string SummaryColor
	{
		get
		{
			if (Results.Count == 0)
				return "#9ca3af";
			int healthy = Results.Count(r => r.IsHealthy);
			return healthy == Results.Count ? "#22c55e" : "#eab308";
		}
	}

	public GameHealthViewModel(GameHealthService health, ToastService toast, SteamAppListCache appList)
	{
		_health = health;
		_toast = toast;
		_appList = appList;
	}

	[RelayCommand]
	private async Task ScanAllAsync()
	{
		if (IsScanning)
			return;

		IsScanning = true;
		Results.Clear();
		ProgressText = "Finding installed games...";

		try
		{
			IReadOnlyList<GameHealthReport> reports = await _health.ScanAllAsync(async (appId, name, current, total) =>
			{
				ProgressText = $"Scanning {name} ({current}/{total})...";
				await Task.Delay(10);
			});

			if (reports.Count == 0)
			{
				ProgressText = "No games found. Check Steam path in Settings.";
			}

			foreach (GameHealthReport report in reports.OrderByDescending(r => r.HealthScore))
			{
				Results.Add(report);
			}

			if (reports.Count > 0)
			{
				ProgressText = $"Scan complete — {Results.Count} games checked.";
			}

			OnPropertyChanged(nameof(SummaryText));
			OnPropertyChanged(nameof(SummaryColor));
			OnPropertyChanged(nameof(HasResults));
		}
		catch (Exception ex)
		{
			ProgressText = $"Scan failed: {ex.Message}";
			_toast.Show("Health Scanner", $"Scan failed: {ex.Message}");
		}
		finally
		{
			IsScanning = false;
		}
	}

	[RelayCommand]
	private async Task FixAllAsync()
	{
		if (IsFixing || Results.Count == 0)
			return;

		IsFixing = true;
		ProgressText = "Applying fixes...";

		try
		{
			(int fixedCount, int failedCount) = await _health.FixAllIssuesAsync(Results);

			if (fixedCount > 0)
			{
				_toast.Show("Health Scanner", $"Fixed {fixedCount} issue(s).{(failedCount > 0 ? $" {failedCount} failed." : "")}");
				await ScanAllAsync();
			}
			else if (failedCount > 0)
			{
				_toast.Show("Health Scanner", $"{failedCount} fix(es) failed — some issues require manual intervention.");
			}
			else
			{
				ProgressText = "No fixable issues found.";
			}
		}
		catch (Exception ex)
		{
			ProgressText = $"Fix failed: {ex.Message}";
			_toast.Show("Health Scanner", $"Fix failed: {ex.Message}");
		}
		finally
		{
			IsFixing = false;
		}
	}

	[RelayCommand]
	private async Task FixSingleAsync(GameHealthReport? report)
	{
		if (report == null)
			return;

		int fixedCount = 0;
		int failedCount = 0;

		foreach (HealthIssue issue in report.Issues.Where(i => i.HasFix))
		{
			if (issue.FixAction == null)
				continue;
			try
			{
				bool ok = await issue.FixAction();
				if (ok) fixedCount++; else failedCount++;
			}
			catch
			{
				failedCount++;
			}
		}

		if (fixedCount > 0)
		{
			_toast.Show("Health Scanner", $"Fixed {fixedCount} issue(s) for {report.GameName}.");
			GameHealthReport updated = await _health.ScanSingleAsync(report.AppId);
			int index = Results.IndexOf(report);
			if (index >= 0)
			{
				Results[index] = updated;
			}
			OnPropertyChanged(nameof(SummaryText));
			OnPropertyChanged(nameof(SummaryColor));
		}
		else if (failedCount > 0)
		{
			_toast.Show("Health Scanner", $"Fix failed for {report.GameName}.");
		}
	}
}
