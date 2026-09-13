using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GabLuchi.Models;
using GabLuchi.Resources;
using GabLuchi.Services;

namespace GabLuchi.ViewModels;

public class MultiplayerFixViewModel : ObservableObject
{
	private readonly MultiplayerFixService _service;
	private readonly OnlineFixService _onlineFix;
	private readonly SteamLibraryService _library;
	private readonly ToastService _toast;

	private string _searchText = "";
	private bool _isBusy;
	private string _statusMessage = "";
	private bool _isDownloading;
	private double _downloadProgress;
	private string _downloadStatus = "";

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

	public string StatusMessage
	{
		get => _statusMessage;
		set => SetProperty(ref _statusMessage, value);
	}

	public bool IsDownloading
	{
		get => _isDownloading;
		set => SetProperty(ref _isDownloading, value);
	}

	public double DownloadProgress
	{
		get => _downloadProgress;
		set => SetProperty(ref _downloadProgress, value);
	}

	public string DownloadStatus
	{
		get => _downloadStatus;
		set => SetProperty(ref _downloadStatus, value);
	}

	public bool CanSearch => !IsBusy && !string.IsNullOrWhiteSpace(SearchText);

	public ObservableCollection<OnlineFixEntry> Results { get; } = new ObservableCollection<OnlineFixEntry>();

	public ICommand SearchCmd => searchCommand ?? (searchCommand = new AsyncRelayCommand(Search));
	private AsyncRelayCommand? searchCommand;

	public ICommand DownloadAndApplyCmd => downloadAndApplyCommand ?? (downloadAndApplyCommand = new AsyncRelayCommand<OnlineFixEntry>(DownloadAndApply));
	private AsyncRelayCommand<OnlineFixEntry>? downloadAndApplyCommand;

	public ICommand SearchOnSiteCmd => searchOnSiteCommand ?? (searchOnSiteCommand = new RelayCommand(SearchOnSite));
	private RelayCommand? searchOnSiteCommand;

	public MultiplayerFixViewModel(MultiplayerFixService service, OnlineFixService onlineFix, SteamLibraryService library, ToastService toast)
	{
		_service = service;
		_onlineFix = onlineFix;
		_library = library;
		_toast = toast;
	}

	private async Task Search()
	{
		if (IsBusy || string.IsNullOrWhiteSpace(SearchText))
		{
			return;
		}
		IsBusy = true;
		StatusMessage = "Searching perondepot...";
		Results.Clear();
		try
		{
			List<OnlineFixEntry> results = await _onlineFix.SearchAsync(SearchText.Trim());
			if (results.Count == 0)
			{
				StatusMessage = Strings.MultiplayerFix_NoResults;
			}
			else
			{
				StatusMessage = string.Format(Strings.MultiplayerFix_Found, results.Count);
				foreach (OnlineFixEntry entry in results)
				{
					Results.Add(entry);
				}
			}
		}
		catch (Exception ex)
		{
			StatusMessage = "Error: " + ex.Message;
			_toast.Show(Strings.MultiplayerFix_Title, ex.Message, error: true);
		}
		finally
		{
			IsBusy = false;
		}
	}

	private async Task DownloadAndApply(OnlineFixEntry? entry)
	{
		if (entry == null || IsDownloading)
		{
			return;
		}
		string? gameDir = _library.GetInstallDir(entry.AppId);
		if (string.IsNullOrEmpty(gameDir) || !Directory.Exists(gameDir))
		{
			_toast.Show(Strings.MultiplayerFix_Title, "Game not found in Steam library. Install it first.", error: true);
			return;
		}
		IsDownloading = true;
		DownloadProgress = 0;
		DownloadStatus = "Downloading...";
		try
		{
			IProgress<double?> progress = new Progress<double?>(p =>
			{
				if (p.HasValue)
				{
					DownloadProgress = p.Value * 100;
				}
				else
				{
					DownloadProgress = -1;
				}
			});
			OnlineFixApplyResult result = await _onlineFix.ApplyFixAsync(entry, gameDir, progress);
			if (result.Success)
			{
				DownloadStatus = "Done! " + result.FilesInstalled + " files installed to " + gameDir;
				DownloadProgress = 100;
				_toast.Show(Strings.MultiplayerFix_Title, "Online fix applied! " + result.FilesInstalled + " files installed.");
			}
			else
			{
				DownloadStatus = "Failed: " + result.Error;
				_toast.Show(Strings.MultiplayerFix_Title, result.Error ?? "Unknown error", error: true);
			}
		}
		catch (Exception ex)
		{
			DownloadStatus = "Failed: " + ex.Message;
			_toast.Show(Strings.MultiplayerFix_Title, ex.Message, error: true);
		}
		finally
		{
			IsDownloading = false;
		}
	}

	private void SearchOnSite()
	{
		string query = string.IsNullOrWhiteSpace(SearchText) ? "" : Uri.EscapeDataString(SearchText.Trim());
		MultiplayerFixService.OpenInBrowser("https://online-fix.me/index.php?do=search&subaction=search&story=" + query);
	}
}
