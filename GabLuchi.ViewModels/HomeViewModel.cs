using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using GabLuchi.Resources;
using GabLuchi.Services;

namespace GabLuchi.ViewModels;

public class HomeViewModel : ObservableObject
{
	private readonly SteamService _steam;

	private readonly SteamAppListCache _appList;

	private readonly SteamAppInfoCache _appInfo;

	private readonly CoverCache _covers;

	private readonly UnlockerService _unlocker;

	private readonly PluginInstallerService _plugin;

	private readonly ToastService _toast;

	private readonly AuthService _auth;

	[ObservableProperty]
	private int _gameCount;

	[ObservableProperty]
	private string _pluginStatusText = Strings.Plugin_Checking;

	[ObservableProperty]
	private string _pluginStatusColor = "#9ca3af";

	[ObservableProperty]
	private bool _showPluginInstall;

	[ObservableProperty]
	[NotifyPropertyChangedFor("NotInstallingPlugin")]
	private bool _isInstallingPlugin;

	[ObservableProperty]
	[NotifyPropertyChangedFor("HasRecent")]
	private ObservableCollection<LuaTileViewModel> _recent = new ObservableCollection<LuaTileViewModel>();

	[ObservableProperty]
	[NotifyPropertyChangedFor("SteamChipText")]
	[NotifyPropertyChangedFor("SteamChipColor")]
	private bool _steamFound;

	[ObservableProperty]
	private string _steamStatus = Strings.Home_CheckingSteam;

	[ObservableProperty]
	[NotifyPropertyChangedFor("IsGuest")]
	private bool _isSignedIn;

	[ObservableProperty]
	private bool _accountShowButton = true;

	[ObservableProperty]
	private string _accountStatus = Strings.Home_BrowsingAsGuest;

	[ObservableProperty]
	private string _modeStatus = Strings.Home_NoModeSelected;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand<LuaTileViewModel>? openGameCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? openPluginCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? openManageCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? openSettingsCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? openModeCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? openAddCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? installPluginCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? signInCommand;

	public Action<long>? NavigateToGame { get; set; }

	public Action? NavigateToPlugin { get; set; }

	public Action? NavigateToManage { get; set; }

	public Action? NavigateToSettings { get; set; }

	public Action? NavigateToMode { get; set; }

	public Action? NavigateToAdd { get; set; }

	public DropInstallViewModel Drop { get; }

	public string SteamChipText => SteamFound ? Strings.Home_SteamReady : Strings.Home_SteamMissing;

	public string SteamChipColor => SteamFound ? "#34d399" : "#f87171";

	public bool NotInstallingPlugin => !IsInstallingPlugin;

	public bool HasRecent => Recent.Count > 0;

	public bool IsGuest => !IsSignedIn;

	public Func<Task>? RequestSignIn { get; set; }

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int GameCount
	{
		get
		{
			return _gameCount;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_gameCount, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.GameCount);
				_gameCount = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.GameCount);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string PluginStatusText
	{
		get
		{
			return _pluginStatusText;
		}
		[MemberNotNull("_pluginStatusText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_pluginStatusText, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PluginStatusText);
				_pluginStatusText = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PluginStatusText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string PluginStatusColor
	{
		get
		{
			return _pluginStatusColor;
		}
		[MemberNotNull("_pluginStatusColor")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_pluginStatusColor, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PluginStatusColor);
				_pluginStatusColor = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PluginStatusColor);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ShowPluginInstall
	{
		get
		{
			return _showPluginInstall;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_showPluginInstall, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ShowPluginInstall);
				_showPluginInstall = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ShowPluginInstall);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsInstallingPlugin
	{
		get
		{
			return _isInstallingPlugin;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isInstallingPlugin, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsInstallingPlugin);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.NotInstallingPlugin);
				_isInstallingPlugin = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsInstallingPlugin);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.NotInstallingPlugin);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<LuaTileViewModel> Recent
	{
		get
		{
			return _recent;
		}
		[MemberNotNull("_recent")]
		set
		{
			if (!EqualityComparer<ObservableCollection<LuaTileViewModel>>.Default.Equals(_recent, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Recent);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HasRecent);
				_recent = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Recent);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HasRecent);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool SteamFound
	{
		get
		{
			return _steamFound;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_steamFound, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SteamFound);
				_steamFound = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SteamFound);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string SteamStatus
	{
		get
		{
			return _steamStatus;
		}
		[MemberNotNull("_steamStatus")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_steamStatus, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SteamStatus);
				_steamStatus = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SteamStatus);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string AccountStatus
	{
		get
		{
			return _accountStatus;
		}
		[MemberNotNull("_accountStatus")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_accountStatus, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.AccountStatus);
				_accountStatus = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AccountStatus);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string ModeStatus
	{
		get
		{
			return _modeStatus;
		}
		[MemberNotNull("_modeStatus")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_modeStatus, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ModeStatus);
				_modeStatus = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ModeStatus);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsSignedIn
	{
		get
		{
			return _isSignedIn;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isSignedIn, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsSignedIn);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsGuest);
				_isSignedIn = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsSignedIn);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsGuest);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool AccountShowButton
	{
		get
		{
			return _accountShowButton;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_accountShowButton, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.AccountShowButton);
				_accountShowButton = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AccountShowButton);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<LuaTileViewModel> OpenGameCommand => openGameCommand ?? (openGameCommand = new RelayCommand<LuaTileViewModel>(OpenGame));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand OpenPluginCommand => openPluginCommand ?? (openPluginCommand = new RelayCommand(OpenPlugin));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand OpenManageCommand => openManageCommand ?? (openManageCommand = new RelayCommand(OpenManage));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand OpenSettingsCommand => openSettingsCommand ?? (openSettingsCommand = new RelayCommand(OpenSettings));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand OpenModeCommand => openModeCommand ?? (openModeCommand = new RelayCommand(OpenMode));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand OpenAddCommand => openAddCommand ?? (openAddCommand = new RelayCommand(OpenAdd));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand InstallPluginCommand => installPluginCommand ?? (installPluginCommand = new AsyncRelayCommand(InstallPlugin));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand SignInCommand => signInCommand ?? (signInCommand = new AsyncRelayCommand(SignIn));

	public HomeViewModel(SteamService steam, SteamAppListCache appList, SteamAppInfoCache appInfo, CoverCache covers, DropInstallViewModel drop, UnlockerService unlocker, PluginInstallerService plugin, ToastService toast, AuthService auth)
	{
		_steam = steam;
		_appList = appList;
		_appInfo = appInfo;
		_covers = covers;
		_unlocker = unlocker;
		_plugin = plugin;
		_toast = toast;
		_auth = auth;
		Drop = drop;
		_auth.AuthStateChanged += RefreshAccount;
		RefreshAccount();
	}

	[RelayCommand]
	private async Task SignIn()
	{
		if (RequestSignIn != null)
		{
			await RequestSignIn();
		}
	}

	[RelayCommand]
	private void OpenGame(LuaTileViewModel tile)
	{
		NavigateToGame?.Invoke(tile.AppId);
	}

	[RelayCommand]
	private void OpenPlugin()
	{
		NavigateToPlugin?.Invoke();
	}

	[RelayCommand]
	private void OpenManage()
	{
		NavigateToManage?.Invoke();
	}

	[RelayCommand]
	private void OpenSettings()
	{
		NavigateToSettings?.Invoke();
	}

	[RelayCommand]
	private void OpenMode()
	{
		NavigateToMode?.Invoke();
	}

	[RelayCommand]
	private void OpenAdd()
	{
		NavigateToAdd?.Invoke();
	}

	[RelayCommand]
	private async Task InstallPlugin()
	{
		if (!IsInstallingPlugin && MessageBox.Show(Strings.Plugin_Confirm_RestartBody, Strings.Plugin_Confirm_RestartCaption, MessageBoxButton.OKCancel, MessageBoxImage.Exclamation) == MessageBoxResult.OK)
		{
			IsInstallingPlugin = true;
			PluginStatusText = Strings.Plugin_Checking;
			PluginStatusColor = "#9ca3af";
			try
			{
				var (flag, arg) = await _plugin.InstallAsync(null);
				_toast.Show(Strings.Plugin_Toast_Title, flag ? Strings.Plugin_Toast_Installed : string.Format(Strings.Plugin_Toast_InstallFailed, arg), !flag);
			}
			finally
			{
				IsInstallingPlugin = false;
				await RefreshPluginStatusAsync();
			}
		}
	}

	public async Task LoadAsync()
	{
		RefreshSteam();
		RefreshMode();
		RefreshPluginStatusAsync();
		await RefreshLibraryAsync();
	}

	private async Task RefreshPluginStatusAsync()
	{
		try
		{
			PluginStatus pluginStatus = await _plugin.GetStatusAsync();
			bool flag = pluginStatus.FrontendInstalled && pluginStatus.DllInstalled;
			ShowPluginInstall = !flag;
			if (flag)
			{
				if (!pluginStatus.UpdateAvailable)
				{
					string installedTag = pluginStatus.InstalledTag;
					string pluginStatusText = ((installedTag != null) ? (Strings.Plugin_Status_Installed + " · " + installedTag) : Strings.Plugin_Status_Installed);
					PluginStatusText = pluginStatusText;
					PluginStatusColor = "#34d399";
				}
				else
				{
					string pluginStatusText = Strings.Plugin_Badge_UpdateAvailable;
					PluginStatusText = pluginStatusText;
					PluginStatusColor = "#fbbf24";
				}
			}
			else
			{
				string pluginStatusText = Strings.Plugin_Status_NotInstalled;
				PluginStatusText = pluginStatusText;
				PluginStatusColor = "#9ca3af";
			}
		}
		catch
		{
		}
	}

	private void RefreshMode()
	{
		string selectedModeDisplayName = _unlocker.SelectedModeDisplayName;
		ModeStatus = ((selectedModeDisplayName != null) ? string.Format(Strings.Home_ModeIs, selectedModeDisplayName) : Strings.Home_NoModeSelected);
	}

	private void RefreshSteam()
	{
		SteamFound = _steam.IsValid;
		SteamStatus = (SteamFound ? string.Format(Strings.Home_SteamDetected, _steam.EffectivePath) : Strings.Home_SteamNotFound);
	}

	private void RefreshAccount()
	{
		IsSignedIn = _auth.IsSignedIn;
		AccountShowButton = !IsSignedIn;
		if (!IsSignedIn)
		{
			AccountStatus = Strings.Home_BrowsingAsGuest;
			return;
		}
		string displayName = _auth.DisplayName;
		AccountStatus = ((displayName != null) ? string.Format(Strings.Home_SignedInAs, displayName) : Strings.Home_SignedIn);
	}

	public async Task RefreshLibraryAsync()
	{
		string dir = _steam.StPlugInDir;
		if (dir == null || !Directory.Exists(dir))
		{
			GameCount = 0;
			Recent = new ObservableCollection<LuaTileViewModel>();
			return;
		}
		await _appList.EnsureLoadedAsync();
		List<LuaTileViewModel> list = await Task.Run(() => (from t in (from path in Directory.EnumerateFiles(dir, "*.lua")
				select (path: path, name: Path.GetFileNameWithoutExtension(path)) into f
				where long.TryParse(f.name, out var _)
				select f).Select<(string, string), LuaTileViewModel>(delegate((string path, string name) f)
			{
				long num = long.Parse(f.name);
				FileInfo fileInfo = new FileInfo(f.path);
				string text = _appList.GetName(num) ?? _appInfo.GetCached(num)?.Name;
				DateTime addedAt = ((fileInfo.LastWriteTime > fileInfo.CreationTime) ? fileInfo.LastWriteTime : fileInfo.CreationTime);
				return new LuaTileViewModel(num, f.path, addedAt, text ?? string.Format(Strings.Common_AppFallback, num), text == null);
			})
			orderby t.AddedAt descending
			select t).ToList());
		GameCount = list.Count;
		List<LuaTileViewModel> list2 = list.Take(4).ToList();
		Recent = new ObservableCollection<LuaTileViewModel>(list2);
		foreach (LuaTileViewModel item in list2)
		{
			item.EnsureResolvedAsync(_appInfo, _covers);
		}
	}
}
