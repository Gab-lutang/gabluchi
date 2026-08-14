using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using GabLuchi.Resources;
using GabLuchi.Services;

namespace GabLuchi.ViewModels;

public class PluginViewModel : ObservableObject
{
	private readonly PluginInstallerService _installer;

	private readonly ToastService _toast;

	[ObservableProperty]
	private string _installedVersion = "—";

	[ObservableProperty]
	private string _latestVersion = "—";

	[ObservableProperty]
	private string _frontendStatus = Strings.Plugin_Checking;

	[ObservableProperty]
	private string _dllStatus = Strings.Plugin_Checking;

	[ObservableProperty]
	private bool _frontendInstalled;

	[ObservableProperty]
	private bool _dllOk;

	[ObservableProperty]
	private bool _dllOutOfDate;

	[ObservableProperty]
	private bool _dllNotInstalled = true;

	[ObservableProperty]
	[NotifyPropertyChangedFor("InstallButtonText")]
	[NotifyPropertyChangedFor("InstallIsPrimary")]
	[NotifyPropertyChangedFor("ShowUpToDate")]
	[NotifyPropertyChangedFor("CanUninstall")]
	private bool _isInstalled;

	[ObservableProperty]
	[NotifyPropertyChangedFor("InstallButtonText")]
	[NotifyPropertyChangedFor("InstallIsPrimary")]
	[NotifyPropertyChangedFor("ShowUpToDate")]
	private bool _updateAvailable;

	[ObservableProperty]
	private bool _millenniumCoexisting;

	[ObservableProperty]
	[NotifyPropertyChangedFor("NotBusy")]
	[NotifyPropertyChangedFor("CanUninstall")]
	private bool _isBusy;

	[ObservableProperty]
	private double _progress;

	[ObservableProperty]
	private bool _isProgressIndeterminate;

	[ObservableProperty]
	private string? _statusLine;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? installCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? uninstallCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? checkForUpdatesCommand;

	public bool InstallIsPrimary
	{
		get
		{
			if (IsInstalled)
			{
				return UpdateAvailable;
			}
			return true;
		}
	}

	public bool ShowUpToDate
	{
		get
		{
			if (IsInstalled)
			{
				return !UpdateAvailable;
			}
			return false;
		}
	}

	public bool NotBusy => !IsBusy;

	public bool CanUninstall
	{
		get
		{
			if (IsInstalled)
			{
				return !IsBusy;
			}
			return false;
		}
	}

	public string InstallButtonText
	{
		get
		{
			if (IsInstalled)
			{
				if (!UpdateAvailable)
				{
					return Strings.Plugin_Btn_Reinstall;
				}
				return Strings.Plugin_Btn_Update;
			}
			return Strings.Plugin_Btn_Install;
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string InstalledVersion
	{
		get
		{
			return _installedVersion;
		}
		[MemberNotNull("_installedVersion")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_installedVersion, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.InstalledVersion);
				_installedVersion = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.InstalledVersion);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string LatestVersion
	{
		get
		{
			return _latestVersion;
		}
		[MemberNotNull("_latestVersion")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_latestVersion, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.LatestVersion);
				_latestVersion = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.LatestVersion);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string FrontendStatus
	{
		get
		{
			return _frontendStatus;
		}
		[MemberNotNull("_frontendStatus")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_frontendStatus, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.FrontendStatus);
				_frontendStatus = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.FrontendStatus);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string DllStatus
	{
		get
		{
			return _dllStatus;
		}
		[MemberNotNull("_dllStatus")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_dllStatus, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.DllStatus);
				_dllStatus = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.DllStatus);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool FrontendInstalled
	{
		get
		{
			return _frontendInstalled;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_frontendInstalled, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.FrontendInstalled);
				_frontendInstalled = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.FrontendInstalled);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool DllOk
	{
		get
		{
			return _dllOk;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_dllOk, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.DllOk);
				_dllOk = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.DllOk);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool DllOutOfDate
	{
		get
		{
			return _dllOutOfDate;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_dllOutOfDate, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.DllOutOfDate);
				_dllOutOfDate = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.DllOutOfDate);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool DllNotInstalled
	{
		get
		{
			return _dllNotInstalled;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_dllNotInstalled, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.DllNotInstalled);
				_dllNotInstalled = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.DllNotInstalled);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsInstalled
	{
		get
		{
			return _isInstalled;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isInstalled, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsInstalled);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.InstallButtonText);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.InstallIsPrimary);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ShowUpToDate);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CanUninstall);
				_isInstalled = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsInstalled);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.InstallButtonText);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.InstallIsPrimary);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ShowUpToDate);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CanUninstall);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool UpdateAvailable
	{
		get
		{
			return _updateAvailable;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_updateAvailable, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.UpdateAvailable);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.InstallButtonText);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.InstallIsPrimary);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ShowUpToDate);
				_updateAvailable = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.UpdateAvailable);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.InstallButtonText);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.InstallIsPrimary);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ShowUpToDate);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool MillenniumCoexisting
	{
		get
		{
			return _millenniumCoexisting;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_millenniumCoexisting, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.MillenniumCoexisting);
				_millenniumCoexisting = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.MillenniumCoexisting);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsBusy
	{
		get
		{
			return _isBusy;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isBusy, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsBusy);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.NotBusy);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CanUninstall);
				_isBusy = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsBusy);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.NotBusy);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CanUninstall);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public double Progress
	{
		get
		{
			return _progress;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(_progress, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Progress);
				_progress = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Progress);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsProgressIndeterminate
	{
		get
		{
			return _isProgressIndeterminate;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isProgressIndeterminate, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsProgressIndeterminate);
				_isProgressIndeterminate = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsProgressIndeterminate);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string? StatusLine
	{
		get
		{
			return _statusLine;
		}
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_statusLine, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.StatusLine);
				_statusLine = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.StatusLine);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand InstallCommand => installCommand ?? (installCommand = new AsyncRelayCommand(Install));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand UninstallCommand => uninstallCommand ?? (uninstallCommand = new AsyncRelayCommand(Uninstall));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand CheckForUpdatesCommand => checkForUpdatesCommand ?? (checkForUpdatesCommand = new AsyncRelayCommand(CheckForUpdates));

	public PluginViewModel(PluginInstallerService installer, ToastService toast)
	{
		_installer = installer;
		_toast = toast;
	}

	public async Task LoadAsync()
	{
		await RefreshAsync(force: false);
	}

	private async Task RefreshAsync(bool force)
	{
		PluginStatus pluginStatus = await _installer.GetStatusAsync(force);
		IsInstalled = pluginStatus.FrontendInstalled && pluginStatus.DllInstalled;
		InstalledVersion = pluginStatus.InstalledTag ?? (IsInstalled ? Strings.Plugin_Version_Unknown : "—");
		LatestVersion = (pluginStatus.Offline ? Strings.Plugin_Version_Offline : (pluginStatus.LatestTag ?? "—"));
		FrontendInstalled = pluginStatus.FrontendInstalled;
		FrontendStatus = (pluginStatus.FrontendInstalled ? Strings.Plugin_Status_Installed : Strings.Plugin_Status_NotInstalled);
		DllOk = pluginStatus.DllInstalled && pluginStatus.DllMatches;
		DllOutOfDate = pluginStatus.DllInstalled && !pluginStatus.DllMatches;
		DllNotInstalled = !pluginStatus.DllInstalled;
		DllStatus = ((!pluginStatus.DllInstalled) ? Strings.Plugin_Status_NotInstalled : (pluginStatus.DllMatches ? Strings.Plugin_Status_UpToDate : Strings.Plugin_Status_OutOfDate));
		UpdateAvailable = pluginStatus.UpdateAvailable;
		MillenniumCoexisting = pluginStatus.MillenniumPresent;
		StatusLine = (pluginStatus.Offline ? Strings.Plugin_Status_OfflineCheck : (pluginStatus.Port8080Busy ? Strings.Plugin_Status_Port8080Busy : null));
	}

	private bool ConfirmSteamRestart()
	{
		return MessageBox.Show(Strings.Plugin_Confirm_RestartBody, Strings.Plugin_Confirm_RestartCaption, MessageBoxButton.OKCancel, MessageBoxImage.Exclamation) == MessageBoxResult.OK;
	}

	private IProgress<double?> MakeProgress()
	{
		return new Progress<double?>(delegate(double? p)
		{
			if (!p.HasValue)
			{
				IsProgressIndeterminate = true;
			}
			else
			{
				IsProgressIndeterminate = false;
				Progress = p.Value * 100.0;
			}
		});
	}

	[RelayCommand]
	private async Task Install()
	{
		if (!IsBusy && ConfirmSteamRestart())
		{
			IsBusy = true;
			IsProgressIndeterminate = true;
			Progress = 0.0;
			try
			{
				var (flag, arg) = await _installer.InstallAsync(MakeProgress());
				_toast.Show(Strings.Plugin_Toast_Title, flag ? Strings.Plugin_Toast_Installed : string.Format(Strings.Plugin_Toast_InstallFailed, arg), !flag);
			}
			finally
			{
				IsBusy = false;
				await RefreshAsync(force: true);
			}
		}
	}

	[RelayCommand]
	private async Task Uninstall()
	{
		if (!IsBusy && IsInstalled && ConfirmSteamRestart())
		{
			IsBusy = true;
			IsProgressIndeterminate = true;
			try
			{
				var (flag, arg) = await _installer.UninstallAsync();
				_toast.Show(Strings.Plugin_Toast_Title, flag ? Strings.Plugin_Toast_Removed : string.Format(Strings.Plugin_Toast_UninstallFailed, arg), !flag);
			}
			finally
			{
				IsBusy = false;
				await RefreshAsync(force: true);
			}
		}
	}

	[RelayCommand]
	private async Task CheckForUpdates()
	{
		if (IsBusy)
		{
			return;
		}
		IsBusy = true;
		IsProgressIndeterminate = true;
		try
		{
			await RefreshAsync(force: true);
			if (StatusLine == null)
			{
				_toast.Show(Strings.Plugin_Toast_Title, UpdateAvailable ? Strings.Plugin_Toast_UpdateAvailable : Strings.Plugin_Toast_UpToDate);
			}
		}
		finally
		{
			IsBusy = false;
		}
	}
}
