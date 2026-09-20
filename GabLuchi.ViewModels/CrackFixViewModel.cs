using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GabLuchi.Models;
using GabLuchi.Resources;
using GabLuchi.Services;
using Microsoft.Win32;

namespace GabLuchi.ViewModels;

public class CrackFixViewModel : ObservableObject
{
	private readonly CrackFixService _crackFix;
	private readonly SteamService _steam;
	private readonly ToastService _toast;
	private readonly AnalyticsService _analytics;

	private List<CrackFixEntry> _allFixes = new List<CrackFixEntry>();
	private CancellationTokenSource? _downloadCts;

	private string _searchText = "";
	private bool _isBusy;
	private bool _isDownloading;
	private double _progress;
	private bool _isProgressIndeterminate;
	private string _statusMessage = "";
	private string _gameDir = "";
	private bool _is7ZipAvailable;

	public bool Is7ZipAvailable
	{
		get => _is7ZipAvailable;
		set => SetProperty(ref _is7ZipAvailable, value);
	}

	public string SearchText
	{
		get => _searchText;
		set { if (SetProperty(ref _searchText, value)) OnPropertyChanged(nameof(CanSearch)); }
	}

	public bool IsBusy
	{
		get => _isBusy;
		set { if (SetProperty(ref _isBusy, value)) OnPropertyChanged(nameof(CanSearch)); }
	}

	public bool IsDownloading
	{
		get => _isDownloading;
		set { if (SetProperty(ref _isDownloading, value)) { OnPropertyChanged(nameof(CanSearch)); OnPropertyChanged(nameof(CanCancel)); } }
	}

	public double Progress
	{
		get => _progress;
		set => SetProperty(ref _progress, value);
	}

	public bool IsProgressIndeterminate
	{
		get => _isProgressIndeterminate;
		set => SetProperty(ref _isProgressIndeterminate, value);
	}

	public string StatusMessage
	{
		get => _statusMessage;
		set => SetProperty(ref _statusMessage, value);
	}

	public string GameDir
	{
		get => _gameDir;
		set => SetProperty(ref _gameDir, value);
	}

	public bool CanSearch => !IsBusy && !IsDownloading;

	public bool CanCancel => IsDownloading;

	public ObservableCollection<CrackFixEntry> Results { get; } = new ObservableCollection<CrackFixEntry>();

	public CrackFixViewModel(CrackFixService crackFix, SteamService steam, ToastService toast, AnalyticsService analytics)
	{
		_crackFix = crackFix;
		_steam = steam;
		_toast = toast;
		_analytics = analytics;
		Is7ZipAvailable = crackFix.Is7ZipAvailable;
	}

	[RelayCommand]
	public async Task Search()
	{
		if (IsBusy || IsDownloading)
		{
			return;
		}
		IsBusy = true;
		StatusMessage = Strings.CrackFix_Loading;
		Results.Clear();
		try
		{
			_allFixes = await _crackFix.FetchFixesAsync();
			List<CrackFixEntry> filtered = _crackFix.Search(SearchText.Trim(), _allFixes);
			StatusMessage = string.Format(Strings.CrackFix_Found, filtered.Count);
			foreach (CrackFixEntry entry in filtered)
			{
				Results.Add(entry);
			}
		}
		catch (Exception ex)
		{
			StatusMessage = ex.Message;
			_toast.Show(Strings.CrackFix_Title, ex.Message, error: true);
		}
		finally
		{
			IsBusy = false;
		}
	}

	[RelayCommand]
	private void BrowseGameDir()
	{
		try
		{
			System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog
			{
				Description = Strings.CrackFix_BrowseGameDir,
				ShowNewFolderButton = false
			};
			System.Windows.Forms.DialogResult result = dlg.ShowDialog();
			if (result == System.Windows.Forms.DialogResult.OK)
			{
				GameDir = dlg.SelectedPath;
			}
		}
		catch (Exception)
		{
		}
	}

	[RelayCommand]
	public async Task DownloadAndApply(CrackFixEntry entry)
	{
		if (IsDownloading || IsBusy)
		{
			return;
		}
		if (entry.Fixes == null || entry.Fixes.Count == 0)
		{
			_toast.Show(Strings.CrackFix_Title, Strings.CrackFix_NoFixes, error: true);
			return;
		}
		CrackFixItem fix = entry.Fixes[0];
		if (string.IsNullOrEmpty(GameDir))
		{
			long appId;
			if (long.TryParse(entry.BuildId, out appId))
			{
				string? dir = _crackFix.GetGameDir(appId);
				if (dir != null)
				{
					GameDir = dir;
				}
			}
			if (string.IsNullOrEmpty(GameDir))
			{
				_toast.Show(Strings.CrackFix_Title, Strings.CrackFix_NoGameDir, error: true);
				return;
			}
		}
		IsDownloading = true;
		IsProgressIndeterminate = true;
		Progress = 0.0;
		StatusMessage = Strings.CrackFix_Downloading;
		_downloadCts = new CancellationTokenSource();
		string tempDir = Path.Combine(Path.GetTempPath(), "GabLuchi", "crackfix_" + Guid.NewGuid().ToString("N")[..8]);
		string extractDir = Path.Combine(tempDir, "extracted");
		try
		{
			Directory.CreateDirectory(tempDir);
			Progress<double?> downloadProgress = new Progress<double?>(p =>
			{
				IsProgressIndeterminate = !p.HasValue;
				if (p.HasValue)
				{
					Progress = p.Value * 100.0;
				}
			});
			string archivePath = await _crackFix.DownloadFixAsync(fix, tempDir, downloadProgress, _downloadCts.Token);
			StatusMessage = Strings.CrackFix_Extracting;
			IsProgressIndeterminate = true;
			if (!_crackFix.ExtractArchive(archivePath, extractDir))
			{
				StatusMessage = Strings.CrackFix_ExtractFailed;
				_toast.Show(Strings.CrackFix_Title, Strings.CrackFix_ExtractFailed, error: true);
				return;
			}
			StatusMessage = Strings.CrackFix_Applying;
			int copied = _crackFix.ApplyToGame(extractDir, GameDir);
			StatusMessage = string.Format(Strings.CrackFix_Done, copied);
			_toast.Show(Strings.CrackFix_Title, string.Format(Strings.CrackFix_Done, copied));
			long.TryParse(entry.BuildId, out long crackAppId);
			if (crackAppId > 0) _ = _analytics.TrackGameFetchAsync(crackAppId, entry.Name ?? "", "crackfix");
		}
		catch (OperationCanceledException)
		{
			StatusMessage = "Cancelled.";
		}
		catch (Exception ex)
		{
			StatusMessage = ex.Message;
			_toast.Show(Strings.CrackFix_Title, ex.Message, error: true);
		}
		finally
		{
			IsDownloading = false;
			IsProgressIndeterminate = false;
			_downloadCts?.Dispose();
			_downloadCts = null;
			try
			{
				Directory.Delete(tempDir, recursive: true);
			}
			catch
			{
			}
		}
	}

	[RelayCommand]
	public void CancelDownload()
	{
		_downloadCts?.Cancel();
	}
}
