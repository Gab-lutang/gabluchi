using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GabLuchi.Models;
using GabLuchi.Resources;
using GabLuchi.Services;

namespace GabLuchi.ViewModels;

public partial class SmartDlcViewModel : ObservableObject
{
	private readonly SmartDlcService _smartDlc;
	private readonly ToastService _toast;

	private ObservableCollection<SmartDlcService.SmartDlcResult> _results = new();
	public ObservableCollection<SmartDlcService.SmartDlcResult> Results
	{
		get => _results;
		set => SetProperty(ref _results, value);
	}

	private bool _isScanning;
	public bool IsScanning
	{
		get => _isScanning;
		set => SetProperty(ref _isScanning, value);
	}

	private bool _isBusy;
	public bool IsBusy
	{
		get => _isBusy;
		set => SetProperty(ref _isBusy, value);
	}

	private string _progressText = string.Empty;
	public string ProgressText
	{
		get => _progressText;
		set => SetProperty(ref _progressText, value);
	}

	public bool HasResults => Results.Count > 0;

	public string SummaryText
	{
		get
		{
			if (Results.Count == 0)
				return string.Empty;
			int withDlc = Results.Count(r => r.DlcIds.Count > 0);
			int alreadyInstalled = Results.Count(r => r.Message?.Contains("Already has") ?? false);
			return $"{Results.Count} games scanned, {withDlc} with DLC, {alreadyInstalled} already unlocked";
		}
	}

	public SmartDlcViewModel(SmartDlcService smartDlc, ToastService toast)
	{
		_smartDlc = smartDlc;
		_toast = toast;
	}

	[RelayCommand]
	private async Task ScanAllAsync()
	{
		if (IsScanning)
			return;

		IsScanning = true;
		Results.Clear();
		ProgressText = "Scanning games for DLC...";

		try
		{
			IReadOnlyList<SmartDlcService.SmartDlcResult> reports = await _smartDlc.ScanAllAsync(async (appId, name, current, total) =>
			{
				ProgressText = $"Checking {name} ({current}/{total})...";
				await Task.Delay(10);
			});

			foreach (SmartDlcService.SmartDlcResult report in reports.OrderByDescending(r => r.DlcIds.Count))
			{
				Results.Add(report);
			}

			ProgressText = $"Scan complete — {Results.Count} games checked.";
			OnPropertyChanged(nameof(SummaryText));
			OnPropertyChanged(nameof(HasResults));
		}
		catch (Exception ex)
		{
			ProgressText = $"Scan failed: {ex.Message}";
			_toast.Show("DLC Scanner", $"Scan failed: {ex.Message}");
		}
		finally
		{
			IsScanning = false;
		}
	}

	[RelayCommand]
	private async Task UnlockAllAsync()
	{
		if (IsBusy || Results.Count == 0)
			return;

		IsBusy = true;
		ProgressText = "Installing unlockers...";

		int success = 0;
		int failed = 0;

		try
		{
			foreach (SmartDlcService.SmartDlcResult result in Results.Where(r => r.RecommendedUnlocker != null && r.DlcIds.Count > 0))
			{
				ProgressText = $"Installing for {result.GameName}...";
				SmartDlcService.SmartDlcResult updated = _smartDlc.InstallForGame(result);
				if (updated.InstallResult?.Success == true)
					success++;
				else
					failed++;
			}

			ProgressText = $"Done — {success} installed, {failed} failed.";
			_toast.Show("DLC Scanner", $"Unlock complete: {success} succeeded, {failed} failed.");
			await ScanAllAsync();
		}
		catch (Exception ex)
		{
			ProgressText = $"Install failed: {ex.Message}";
			_toast.Show("DLC Scanner", $"Install failed: {ex.Message}");
		}
		finally
		{
			IsBusy = false;
		}
	}

	[RelayCommand]
	private async Task InstallSingleAsync(SmartDlcService.SmartDlcResult? result)
	{
		if (result == null || result.RecommendedUnlocker == null)
			return;

		IsBusy = true;
		try
		{
			SmartDlcService.SmartDlcResult updated = _smartDlc.InstallForGame(result);
			if (updated.InstallResult?.Success == true)
			{
				_toast.Show("DLC Scanner", $"Installed {updated.RecommendedUnlocker.DisplayName} for {result.GameName}.");
				int index = Results.IndexOf(result);
				if (index >= 0)
					Results[index] = updated;
				OnPropertyChanged(nameof(SummaryText));
			}
			else
			{
				_toast.Show("DLC Scanner", $"Failed: {updated.Message}");
			}
		}
		catch (Exception ex)
		{
			_toast.Show("DLC Scanner", $"Error: {ex.Message}");
		}
		finally
		{
			IsBusy = false;
		}
	}
}
