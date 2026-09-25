using System;
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

public class OlderVersionViewModel : ObservableObject
{
	private readonly DepotDownloaderModService _downloader;
	private readonly ToastService _toast;
	private readonly SteamService _steam;

	private CancellationTokenSource? _downloadCts;
	private int _appId;
	private int _depotId;
	private string _manifestId = "";
	private string _depotKey = "";
	private string _destDir = "";
	private bool _isDownloading;
	private double _progress;
	private bool _isProgressIndeterminate;
	private string _statusMessage = "";
	private bool _isDotNet9Available;
	private bool _isToolPresent;

	public int AppId
	{
		get => _appId;
		set { if (SetProperty(ref _appId, value)) OnPropertyChanged(nameof(CanDownload)); }
	}

	public int DepotId
	{
		get => _depotId;
		set => SetProperty(ref _depotId, value);
	}

	public string ManifestId
	{
		get => _manifestId;
		set => SetProperty(ref _manifestId, value);
	}

	public string DepotKey
	{
		get => _depotKey;
		set => SetProperty(ref _depotKey, value);
	}

	public string DestDir
	{
		get => _destDir;
		set => SetProperty(ref _destDir, value);
	}

	public bool IsDownloading
	{
		get => _isDownloading;
		set { if (SetProperty(ref _isDownloading, value)) OnPropertyChanged(nameof(CanDownload)); }
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

	public bool IsDotNet9Available
	{
		get => _isDotNet9Available;
		set { if (SetProperty(ref _isDotNet9Available, value)) OnPropertyChanged(nameof(CanDownload)); }
	}

	public bool IsToolPresent
	{
		get => _isToolPresent;
		set { if (SetProperty(ref _isToolPresent, value)) OnPropertyChanged(nameof(CanDownload)); }
	}

	public bool CanDownload => !IsDownloading && IsDotNet9Available && IsToolPresent;

	public OlderVersionViewModel(DepotDownloaderModService downloader, ToastService toast, SteamService steam)
	{
		_downloader = downloader;
		_toast = toast;
		_steam = steam;
		IsDotNet9Available = downloader.IsDotNet9Available;
		IsToolPresent = downloader.IsToolPresent;
		DestDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "GabLuchi");
	}

	[RelayCommand]
	private async Task CheckDotNet()
	{
		IsDotNet9Available = await _downloader.CheckDotNet9Async();
		OnPropertyChanged(nameof(CanDownload));
		_toast.Show(
			Strings.OlderVersion_Title,
			IsDotNet9Available ? Strings.OlderVersion_DotNetAvailable : Strings.OlderVersion_DotNetMissing,
			!IsDotNet9Available);
	}

	[RelayCommand]
	private void OpenSteamDbForApp()
	{
		if (AppId > 0)
		{
			SteamService.OpenUrl($"https://steamdb.info/app/{AppId}/depots/");
		}
	}

	[RelayCommand]
	private void DownloadDotNet9()
	{
		SteamService.OpenUrl("https://dotnet.microsoft.com/download/dotnet/9.0");
	}

	[RelayCommand]
	private void BrowseDest()
	{
		OpenFolderDialog dlg = new OpenFolderDialog
		{
			Title = Strings.OlderVersion_BrowseDest,
			InitialDirectory = DestDir
		};
		if (dlg.ShowDialog() == true)
		{
			DestDir = dlg.FolderName;
		}
	}

	[RelayCommand]
	private async Task Download()
	{
		if (IsDownloading || AppId <= 0 || DepotId <= 0 || string.IsNullOrWhiteSpace(ManifestId))
		{
			return;
		}
		IsDownloading = true;
		IsProgressIndeterminate = true;
		Progress = 0.0;
		StatusMessage = Strings.OlderVersion_Downloading;
		_downloadCts = new CancellationTokenSource();
		try
		{
			DepotDownloadResult result = await _downloader.DownloadDepotAsync(
				AppId, DepotId, ManifestId,
				string.IsNullOrWhiteSpace(DepotKey) ? null : DepotKey,
				DestDir,
				new Progress<double?>(p =>
				{
					if (p.HasValue)
					{
						IsProgressIndeterminate = false;
						Progress = p.Value * 100.0;
					}
				}),
				_downloadCts.Token);
			if (result.Success)
			{
				StatusMessage = Strings.OlderVersion_Done;
				Progress = 100.0;
				_toast.Show(Strings.OlderVersion_Title, Strings.OlderVersion_Done);
			}
			else
			{
				StatusMessage = result.Error ?? Strings.OlderVersion_Error;
				_toast.Show(Strings.OlderVersion_Title, result.Error ?? Strings.OlderVersion_Error, error: true);
			}
		}
		catch (OperationCanceledException)
		{
			StatusMessage = "Cancelled.";
		}
		catch (Exception ex)
		{
			StatusMessage = ex.Message;
			_toast.Show(Strings.OlderVersion_Title, ex.Message, error: true);
		}
		finally
		{
			IsDownloading = false;
			IsProgressIndeterminate = false;
			_downloadCts?.Dispose();
			_downloadCts = null;
		}
	}

	[RelayCommand]
	private void CancelDownload()
	{
		_downloadCts?.Cancel();
	}
}
