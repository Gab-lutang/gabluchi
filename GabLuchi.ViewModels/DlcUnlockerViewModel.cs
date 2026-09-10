using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GabLuchi.Models;
using GabLuchi.Resources;
using GabLuchi.Services;
using Microsoft.Win32;

namespace GabLuchi.ViewModels;

public class DlcUnlockerViewModel : ObservableObject
{
	private readonly DlcUnlockerManager _manager;
	private readonly ToastService _toast;

	private string _gameDir = "";
	private long _appId;
	private string _appIdText = "";
	private string _dlcIdsText = "";
	private string _statusMessage = "";
	private bool _isBusy;
	private bool _isInstalling;
	private string _detectedPlatform = "";
	private DlcUnlockerBase? _selectedUnlocker;
	private DlcUnlockerBase? _installedUnlocker;

	public string GameDir
	{
		get => _gameDir;
		set { if (SetProperty(ref _gameDir, value)) { OnPropertyChanged(nameof(CanDetect)); OnPropertyChanged(nameof(CanInstall)); } }
	}

	public string AppIdText
	{
		get => _appIdText;
		set { if (SetProperty(ref _appIdText, value)) { long.TryParse(value, out _appId); OnPropertyChanged(nameof(CanFetchDlcs)); OnPropertyChanged(nameof(CanInstall)); } }
	}

	public long AppId
	{
		get => _appId;
		set { if (SetProperty(ref _appId, value)) { _appIdText = value.ToString(); OnPropertyChanged(nameof(AppIdText)); OnPropertyChanged(nameof(CanFetchDlcs)); OnPropertyChanged(nameof(CanInstall)); } }
	}

	public string DlcIdsText
	{
		get => _dlcIdsText;
		set => SetProperty(ref _dlcIdsText, value);
	}

	public string StatusMessage
	{
		get => _statusMessage;
		set => SetProperty(ref _statusMessage, value);
	}

	public string DetectedPlatform
	{
		get => _detectedPlatform;
		set => SetProperty(ref _detectedPlatform, value);
	}

	public bool IsBusy
	{
		get => _isBusy;
		set { if (SetProperty(ref _isBusy, value)) { OnPropertyChanged(nameof(CanDetect)); OnPropertyChanged(nameof(CanFetchDlcs)); OnPropertyChanged(nameof(CanInstall)); } }
	}

	public bool IsInstalling
	{
		get => _isInstalling;
		set { if (SetProperty(ref _isInstalling, value)) OnPropertyChanged(nameof(CanInstall)); }
	}

	public DlcUnlockerBase? SelectedUnlocker
	{
		get => _selectedUnlocker;
		set { if (SetProperty(ref _selectedUnlocker, value)) OnPropertyChanged(nameof(CanInstall)); }
	}

	public DlcUnlockerBase? InstalledUnlocker
	{
		get => _installedUnlocker;
		set { if (SetProperty(ref _installedUnlocker, value)) OnPropertyChanged(nameof(IsInstalled)); OnPropertyChanged(nameof(CanInstall)); OnPropertyChanged(nameof(CanUninstall)); }
	}

	public bool IsInstalled => InstalledUnlocker != null;

	public bool HasCompatibleUnlockers => CompatibleUnlockers.Count > 0;

	public bool CanDetect => !IsBusy && !string.IsNullOrEmpty(GameDir);

	public bool CanFetchDlcs => !IsBusy && AppId > 0;

	public bool CanInstall => !IsBusy && !IsInstalling && !string.IsNullOrEmpty(GameDir) && AppId > 0 && SelectedUnlocker != null;

	public bool CanUninstall => !IsBusy && !IsInstalling && IsInstalled;

	public ObservableCollection<DlcUnlockerBase> CompatibleUnlockers { get; } = new ObservableCollection<DlcUnlockerBase>();

	public DlcUnlockerViewModel(DlcUnlockerManager manager, ToastService toast)
	{
		_manager = manager;
		_toast = toast;
	}

	[RelayCommand]
	private void BrowseGameDir()
	{
		try
		{
			System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog
			{
				Description = Strings.DlcUnlocker_BrowseGameDir,
				ShowNewFolderButton = false
			};
			System.Windows.Forms.DialogResult result = dlg.ShowDialog();
			if (result == System.Windows.Forms.DialogResult.OK)
			{
				GameDir = dlg.SelectedPath;
			}
		}
		catch (Exception ex)
		{
			StatusMessage = ex.Message;
		}
	}

	[RelayCommand]
	public async Task DetectPlatform()
	{
		if (string.IsNullOrEmpty(GameDir) || !Directory.Exists(GameDir))
		{
			StatusMessage = Strings.DlcUnlocker_InvalidDir;
			return;
		}
		IsBusy = true;
		StatusMessage = Strings.DlcUnlocker_Detecting;
		CompatibleUnlockers.Clear();
		SelectedUnlocker = null;
		InstalledUnlocker = null;
		OnPropertyChanged(nameof(HasCompatibleUnlockers));
		try
		{
			string? platform = _manager.DetectPlatform(GameDir);
			DetectedPlatform = platform switch
			{
				"steam" => "Steam",
				"ubisoft_r1" => "Ubisoft Connect (R1)",
				"ubisoft_r2" => "Ubisoft Connect (R2)",
				_ => ""
			};
			if (platform == null)
			{
				StatusMessage = Strings.DlcUnlocker_NoPlatform;
				return;
			}
			List<DlcUnlockerBase> compatible = _manager.GetCompatibleUnlockers(platform);
			foreach (DlcUnlockerBase u in compatible)
			{
				CompatibleUnlockers.Add(u);
			}
			OnPropertyChanged(nameof(HasCompatibleUnlockers));
			DlcUnlockerBase? installed = _manager.GetInstalledUnlocker(GameDir);
			InstalledUnlocker = installed;
			if (installed != null)
			{
				StatusMessage = string.Format(Strings.DlcUnlocker_AlreadyInstalled, installed.DisplayName);
				SelectedUnlocker = installed;
			}
			else
			{
				StatusMessage = string.Format(Strings.DlcUnlocker_PlatformDetected, DetectedPlatform, compatible.Count);
			}
			long? detectedAppId = _manager.DetectAppId(GameDir);
			if (detectedAppId.HasValue && AppId == 0)
			{
				AppId = detectedAppId.Value;
				StatusMessage += $" | AppID: {detectedAppId}";
			}
		}
		catch (Exception ex)
		{
			StatusMessage = ex.Message;
		}
		finally
		{
			IsBusy = false;
		}
	}

	[RelayCommand]
	public async Task FetchDlcIds()
	{
		if (AppId <= 0)
		{
			return;
		}
		IsBusy = true;
		StatusMessage = Strings.DlcUnlocker_FetchingDlcs;
		try
		{
			List<long> dlcIds = await _manager.FetchDlcIdsAsync(AppId);
			if (dlcIds.Count == 0)
			{
				DlcIdsText = "";
				StatusMessage = Strings.DlcUnlocker_NoDlcsFound;
			}
			else
			{
				DlcIdsText = string.Join(", ", dlcIds);
				string note = dlcIds.Count >= 10 ? " (max 10 from API, add more manually if needed)" : "";
				StatusMessage = string.Format(Strings.DlcUnlocker_DlcsFetched, dlcIds.Count) + note;
			}
		}
		catch (Exception ex)
		{
			StatusMessage = ex.Message;
		}
		finally
		{
			IsBusy = false;
		}
	}

	[RelayCommand]
	private void AddDlcId(string id)
	{
		if (string.IsNullOrWhiteSpace(id)) return;
		long parsed;
		if (!long.TryParse(id.Trim(), out parsed)) return;
		List<long> current = ParseDlcIds();
		if (!current.Contains(parsed))
		{
			current.Add(parsed);
			DlcIdsText = string.Join(", ", current);
		}
	}

	[RelayCommand]
	public async Task Install()
	{
		if (SelectedUnlocker == null || string.IsNullOrEmpty(GameDir) || AppId <= 0)
		{
			return;
		}
		List<long> dlcIds = ParseDlcIds();
		if (dlcIds.Count == 0)
		{
			StatusMessage = Strings.DlcUnlocker_FetchingDlcs;
			try
			{
				dlcIds = await _manager.FetchDlcIdsAsync(AppId);
				if (dlcIds.Count > 0)
				{
					DlcIdsText = string.Join(", ", dlcIds);
				}
			}
			catch
			{
			}
		}
		IsInstalling = true;
		StatusMessage = string.Format(Strings.DlcUnlocker_Installing, SelectedUnlocker.DisplayName);
		try
		{
			DlcUnlockerInstallResult result = await System.Threading.Tasks.Task.Run(() =>
				_manager.Install(SelectedUnlocker.Type, GameDir, dlcIds, AppId));
			if (result.Success)
			{
				StatusMessage = string.Format(Strings.DlcUnlocker_Installed, result.DlcCount);
				_toast.Show(Strings.DlcUnlocker_Title, StatusMessage);
				InstalledUnlocker = _manager.GetInstalledUnlocker(GameDir);
			}
			else
			{
				StatusMessage = result.Error ?? Strings.DlcUnlocker_InstallFailed;
				_toast.Show(Strings.DlcUnlocker_Title, StatusMessage, error: true);
			}
		}
		catch (Exception ex)
		{
			StatusMessage = ex.Message;
			_toast.Show(Strings.DlcUnlocker_Title, ex.Message, error: true);
		}
		finally
		{
			IsInstalling = false;
		}
	}

	[RelayCommand]
	public async Task Uninstall()
	{
		if (string.IsNullOrEmpty(GameDir))
		{
			return;
		}
		IsInstalling = true;
		StatusMessage = Strings.DlcUnlocker_Uninstalling;
		try
		{
			bool ok = await System.Threading.Tasks.Task.Run(() => _manager.Uninstall(GameDir));
			if (ok)
			{
				StatusMessage = Strings.DlcUnlocker_Uninstalled;
				_toast.Show(Strings.DlcUnlocker_Title, StatusMessage);
				InstalledUnlocker = null;
			}
			else
			{
				StatusMessage = Strings.DlcUnlocker_UninstallFailed;
				_toast.Show(Strings.DlcUnlocker_Title, StatusMessage, error: true);
			}
		}
		catch (Exception ex)
		{
			StatusMessage = ex.Message;
			_toast.Show(Strings.DlcUnlocker_Title, ex.Message, error: true);
		}
		finally
		{
			IsInstalling = false;
		}
	}

	private List<long> ParseDlcIds()
	{
		if (string.IsNullOrWhiteSpace(DlcIdsText))
		{
			return new List<long>();
		}
		return DlcIdsText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Select(s => long.TryParse(s, out long id) ? id : -1)
			.Where(id => id > 0)
			.ToList();
	}
}
