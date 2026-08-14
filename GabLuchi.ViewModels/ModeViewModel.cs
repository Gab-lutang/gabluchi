using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using GabLuchi.Models;
using GabLuchi.Resources;
using GabLuchi.Services;

namespace GabLuchi.ViewModels;

public class ModeViewModel : ObservableObject
{
	private readonly UnlockerService _unlocker;

	private readonly ToastService _toast;

	private readonly SteamService _steam;

	private readonly CloudRedirectService _cloudRedirect;

	[ObservableProperty]
	[NotifyPropertyChangedFor("NotBusy")]
	[NotifyPropertyChangedFor("CanUseCloudRedirect")]
	private bool _isBusy;

	[ObservableProperty]
	private double _progress;

	[ObservableProperty]
	private bool _isProgressIndeterminate;

	[ObservableProperty]
	private bool _isConfirming;

	[ObservableProperty]
	private string _confirmTitle = "";

	private ModeCardViewModel? _pendingCard;

	private const string CloudRedirectTitle = "CloudRedirect";

	[ObservableProperty]
	[NotifyPropertyChangedFor("ShowCloudRedirectManage")]
	[NotifyPropertyChangedFor("ShowCloudRedirectUpdate")]
	[NotifyPropertyChangedFor("CanUseCloudRedirect")]
	private bool _cloudRedirectUnlocked;

	[ObservableProperty]
	[NotifyPropertyChangedFor("CloudRedirectToggleText")]
	[NotifyPropertyChangedFor("ShowCloudRedirectManage")]
	private bool _cloudRedirectEnabled;

	[ObservableProperty]
	private bool _cloudRedirectInstalled;

	[ObservableProperty]
	[NotifyPropertyChangedFor("ShowCloudRedirectUpdate")]
	private bool _cloudRedirectUpdateAvailable;

	[ObservableProperty]
	private string _cloudRedirectStatusText = "";

	private bool _detectionAttempted;

	private DateTime _lastCheck;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? manageCloudRedirectCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? toggleCloudRedirectCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? updateCloudRedirectCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? checkForUpdatesCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand<ModeCardViewModel>? installCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? cancelConfirmCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? confirmInstallCommand;

	public ObservableCollection<ModeCardViewModel> Cards { get; } = new ObservableCollection<ModeCardViewModel>();

	public bool NotBusy => !IsBusy;

	public bool CanUseCloudRedirect
	{
		get
		{
			if (CloudRedirectUnlocked)
			{
				return !IsBusy;
			}
			return false;
		}
	}

	public string CloudRedirectToggleText
	{
		get
		{
			if (!CloudRedirectEnabled)
			{
				return Strings.Mode_CloudRedirect_Enable;
			}
			return Strings.Mode_CloudRedirect_Disable;
		}
	}

	public bool ShowCloudRedirectUpdate
	{
		get
		{
			if (CloudRedirectUnlocked)
			{
				return CloudRedirectUpdateAvailable;
			}
			return false;
		}
	}

	public bool ShowCloudRedirectManage
	{
		get
		{
			if (CloudRedirectUnlocked)
			{
				return CloudRedirectEnabled;
			}
			return false;
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
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CanUseCloudRedirect);
				_isBusy = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsBusy);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.NotBusy);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CanUseCloudRedirect);
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
	public bool IsConfirming
	{
		get
		{
			return _isConfirming;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isConfirming, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsConfirming);
				_isConfirming = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsConfirming);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string ConfirmTitle
	{
		get
		{
			return _confirmTitle;
		}
		[MemberNotNull("_confirmTitle")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_confirmTitle, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ConfirmTitle);
				_confirmTitle = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ConfirmTitle);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool CloudRedirectUnlocked
	{
		get
		{
			return _cloudRedirectUnlocked;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_cloudRedirectUnlocked, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CloudRedirectUnlocked);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ShowCloudRedirectManage);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ShowCloudRedirectUpdate);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CanUseCloudRedirect);
				_cloudRedirectUnlocked = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CloudRedirectUnlocked);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ShowCloudRedirectManage);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ShowCloudRedirectUpdate);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CanUseCloudRedirect);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool CloudRedirectEnabled
	{
		get
		{
			return _cloudRedirectEnabled;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_cloudRedirectEnabled, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CloudRedirectEnabled);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CloudRedirectToggleText);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ShowCloudRedirectManage);
				_cloudRedirectEnabled = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CloudRedirectEnabled);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CloudRedirectToggleText);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ShowCloudRedirectManage);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool CloudRedirectInstalled
	{
		get
		{
			return _cloudRedirectInstalled;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_cloudRedirectInstalled, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CloudRedirectInstalled);
				_cloudRedirectInstalled = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CloudRedirectInstalled);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool CloudRedirectUpdateAvailable
	{
		get
		{
			return _cloudRedirectUpdateAvailable;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_cloudRedirectUpdateAvailable, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CloudRedirectUpdateAvailable);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ShowCloudRedirectUpdate);
				_cloudRedirectUpdateAvailable = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CloudRedirectUpdateAvailable);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ShowCloudRedirectUpdate);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string CloudRedirectStatusText
	{
		get
		{
			return _cloudRedirectStatusText;
		}
		[MemberNotNull("_cloudRedirectStatusText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_cloudRedirectStatusText, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CloudRedirectStatusText);
				_cloudRedirectStatusText = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CloudRedirectStatusText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ManageCloudRedirectCommand => manageCloudRedirectCommand ?? (manageCloudRedirectCommand = new AsyncRelayCommand(ManageCloudRedirect));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ToggleCloudRedirectCommand => toggleCloudRedirectCommand ?? (toggleCloudRedirectCommand = new AsyncRelayCommand(ToggleCloudRedirect));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand UpdateCloudRedirectCommand => updateCloudRedirectCommand ?? (updateCloudRedirectCommand = new AsyncRelayCommand(UpdateCloudRedirect));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand CheckForUpdatesCommand => checkForUpdatesCommand ?? (checkForUpdatesCommand = new AsyncRelayCommand(CheckForUpdates));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<ModeCardViewModel> InstallCommand => installCommand ?? (installCommand = new RelayCommand<ModeCardViewModel>(Install));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand CancelConfirmCommand => cancelConfirmCommand ?? (cancelConfirmCommand = new RelayCommand(CancelConfirm));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ConfirmInstallCommand => confirmInstallCommand ?? (confirmInstallCommand = new AsyncRelayCommand(ConfirmInstall));

	public ModeViewModel(UnlockerService unlocker, ToastService toast, SteamService steam, CloudRedirectService cloudRedirect)
	{
		_unlocker = unlocker;
		_toast = toast;
		_steam = steam;
		_cloudRedirect = cloudRedirect;
	}

	[RelayCommand]
	private async Task ManageCloudRedirect()
	{
		if (IsBusy)
		{
			return;
		}
		IsBusy = true;
		IsProgressIndeterminate = true;
		Progress = 0.0;
		try
		{
			Progress<double?> progress = new Progress<double?>(delegate(double? p)
			{
				IsProgressIndeterminate = !p.HasValue;
				if (p.HasValue)
				{
					Progress = p.Value * 100.0;
				}
			});
			if (!(await _cloudRedirect.LaunchAsync(progress)))
			{
				_toast.Show(Strings.Mode_CloudRedirect_Manage, Strings.Mode_CloudRedirect_LaunchFailed, error: true);
			}
		}
		finally
		{
			IsBusy = false;
			IsProgressIndeterminate = false;
		}
	}

	private async Task RefreshCloudRedirectAsync(bool forceRefresh)
	{
		CloudRedirectUnlocked = _unlocker.SelectedMode == UnlockerMode.OpenSteamToolsNightly;
		CloudRedirectAddonState cloudRedirectAddonState = await _unlocker.GetCloudRedirectStateAsync(CloudRedirectUnlocked, forceRefresh);
		CloudRedirectInstalled = cloudRedirectAddonState.Installed;
		CloudRedirectEnabled = cloudRedirectAddonState.Enabled;
		CloudRedirectUpdateAvailable = cloudRedirectAddonState.UpdateAvailable;
		CloudRedirectStatusText = ((!CloudRedirectUnlocked) ? Strings.Mode_CloudRedirect_Locked : ((!cloudRedirectAddonState.Installed) ? Strings.Mode_CloudRedirect_Status_NotInstalled : (cloudRedirectAddonState.UpdateAvailable ? Strings.Mode_CloudRedirect_Status_UpdateAvailable : (cloudRedirectAddonState.Enabled ? Strings.Mode_CloudRedirect_Status_Enabled : Strings.Mode_CloudRedirect_Status_Disabled))));
	}

	[RelayCommand]
	private async Task ToggleCloudRedirect()
	{
		if (IsBusy || !CloudRedirectUnlocked)
		{
			return;
		}
		bool enabling = !CloudRedirectEnabled;
		IsBusy = true;
		IsProgressIndeterminate = true;
		Progress = 0.0;
		try
		{
			Progress<double?> progress = new Progress<double?>(delegate(double? p)
			{
				IsProgressIndeterminate = !p.HasValue;
				if (p.HasValue)
				{
					Progress = p.Value * 100.0;
				}
			});
			ModeInstallResult modeInstallResult = ((!enabling) ? _unlocker.DisableCloudRedirect() : (await _unlocker.EnableCloudRedirectAsync(progress)));
			ModeInstallResult modeInstallResult2 = modeInstallResult;
			if (modeInstallResult2.Success)
			{
				_toast.Show("CloudRedirect", enabling ? Strings.Mode_CloudRedirect_Toast_Enabled : Strings.Mode_CloudRedirect_Toast_Disabled);
			}
			else
			{
				_toast.Show("CloudRedirect", modeInstallResult2.Error ?? "", error: true);
			}
			await LoadAsync();
		}
		finally
		{
			IsBusy = false;
			IsProgressIndeterminate = false;
		}
	}

	[RelayCommand]
	private async Task UpdateCloudRedirect()
	{
		if (IsBusy || !CloudRedirectUnlocked)
		{
			return;
		}
		IsBusy = true;
		IsProgressIndeterminate = true;
		Progress = 0.0;
		try
		{
			Progress<double?> progress = new Progress<double?>(delegate(double? p)
			{
				IsProgressIndeterminate = !p.HasValue;
				if (p.HasValue)
				{
					Progress = p.Value * 100.0;
				}
			});
			ModeInstallResult modeInstallResult = await _unlocker.UpdateCloudRedirectAsync(progress);
			if (modeInstallResult.Success)
			{
				_toast.Show("CloudRedirect", Strings.Mode_CloudRedirect_Toast_Updated);
			}
			else
			{
				_toast.Show("CloudRedirect", modeInstallResult.Error ?? "", error: true);
			}
			await LoadAsync();
		}
		finally
		{
			IsBusy = false;
			IsProgressIndeterminate = false;
		}
	}

	private bool IsModeVisible(ModeDefinition def)
	{
		if (def.HiddenUnlessFile == null)
		{
			return true;
		}
		if (_unlocker.SelectedMode == def.Mode)
		{
			return true;
		}
		string effectivePath = _steam.EffectivePath;
		if (effectivePath != null)
		{
			return File.Exists(Path.Combine(effectivePath, def.HiddenUnlessFile));
		}
		return false;
	}

	private void SyncCards()
	{
		List<ModeDefinition> list = _unlocker.Modes.Where((ModeDefinition d) => d.Mode != UnlockerMode.CloudRedirect).Where(IsModeVisible).ToList();
		int i;
		for (i = Cards.Count - 1; i >= 0; i--)
		{
			if (!list.Any((ModeDefinition d) => d.Mode == Cards[i].Mode))
			{
				Cards.RemoveAt(i);
			}
		}
		foreach (ModeDefinition def in list)
		{
			if (!Cards.Any((ModeCardViewModel c) => c.Mode == def.Mode))
			{
				int val = list.IndexOf(def);
				val = Math.Min(val, Cards.Count);
				Cards.Insert(val, new ModeCardViewModel(def.Mode, def.DisplayName, def.Description));
			}
		}
	}

	public async Task LoadAsync(bool forceRefresh = false)
	{
		if (!_detectionAttempted && !_unlocker.SelectedMode.HasValue)
		{
			_detectionAttempted = true;
			await _unlocker.DetectActiveModeAsync();
		}
		SyncCards();
		UnlockerMode? active = _unlocker.SelectedMode;
		foreach (ModeCardViewModel card2 in Cards)
		{
			if (card2.Mode == active)
			{
				card2.StatusText = Strings.Mode_Checking;
				ModeCardViewModel card = card2;
				Apply(card, await _unlocker.GetStateAsync(card2.Mode, forceRefresh));
			}
			else
			{
				Apply(card2, new ModeState(card2.Mode, ModeStatus.NotInstalled, IsActive: false, null));
			}
		}
		await RefreshCloudRedirectAsync(forceRefresh);
	}

	[RelayCommand]
	private async Task CheckForUpdates()
	{
		if (!IsBusy && !(DateTime.UtcNow - _lastCheck < TimeSpan.FromSeconds(30.0)))
		{
			_lastCheck = DateTime.UtcNow;
			await LoadAsync(forceRefresh: true);
		}
	}

	private void Apply(ModeCardViewModel card, ModeState s)
	{
		ModeCardViewModel modeCardViewModel;
		if (s.Status == ModeStatus.Unknown)
		{
			card.StatusText = Strings.Mode_StatusUnavailable;
		}
		else if (!s.IsActive)
		{
			card.StatusText = Strings.Mode_NotActive;
		}
		else
		{
			modeCardViewModel = card;
			modeCardViewModel.StatusText = s.Status switch
			{
				ModeStatus.NotInstalled => Strings.Mode_NotInstalled, 
				ModeStatus.UpToDate => Strings.Mode_UpToDate, 
				ModeStatus.UpdateAvailable => Strings.Mode_UpdateAvailable, 
				_ => Strings.Mode_StatusUnavailable, 
			};
		}
		modeCardViewModel = card;
		bool isActive = s.IsActive;
		ModeStatus status = s.Status;
		string buttonText = ((!isActive) ? Strings.Mode_Btn_Switch : (status switch
		{
			ModeStatus.UpToDate => Strings.Mode_Btn_Reinstall, 
			ModeStatus.UpdateAvailable => Strings.Mode_Btn_Update, 
			_ => Strings.Mode_Btn_Install, 
		}));
		modeCardViewModel.ButtonText = buttonText;
		card.IsActive = s.IsActive;
	}

	[RelayCommand]
	private void Install(ModeCardViewModel card)
	{
		if (!IsBusy)
		{
			_pendingCard = card;
			ConfirmTitle = (card.IsActive ? string.Format(Strings.Mode_Confirm_Reinstall, card.Title) : string.Format(Strings.Mode_Confirm_Switch, card.Title));
			IsConfirming = true;
		}
	}

	[RelayCommand]
	private void CancelConfirm()
	{
		IsConfirming = false;
		_pendingCard = null;
	}

	[RelayCommand]
	private async Task ConfirmInstall()
	{
		IsConfirming = false;
		ModeCardViewModel pendingCard = _pendingCard;
		_pendingCard = null;
		if (pendingCard != null)
		{
			await RunInstall(pendingCard.Mode);
		}
	}

	private async Task RunInstall(UnlockerMode mode)
	{
		if (IsBusy)
		{
			return;
		}
		IsBusy = true;
		IsProgressIndeterminate = true;
		Progress = 0.0;
		try
		{
			Progress<double?> prog = new Progress<double?>(delegate(double? p)
			{
				IsProgressIndeterminate = !p.HasValue;
				if (p.HasValue)
				{
					Progress = p.Value * 100.0;
				}
			});
			await Task.Run((Action)_steam.StopSteam);
			ModeInstallResult result = await _unlocker.InstallAsync(mode, prog);
			if (result.Success)
			{
				bool flag = await Task.Run((Func<bool>)_steam.StartSteam);
				_toast.Show(Strings.Mode_Toast_Updated, flag ? string.Format(Strings.Mode_Toast_Updated_Restarting, mode) : string.Format(Strings.Mode_Toast_Updated_Start, mode));
			}
			else
			{
				await Task.Run((Func<bool>)_steam.StartSteam);
				_toast.Show(Strings.Mode_Toast_InstallFailed, result.Error ?? Strings.Mode_Toast_InstallFailed_Body, error: true);
			}
			await LoadAsync();
		}
		finally
		{
			IsBusy = false;
			IsProgressIndeterminate = false;
		}
	}
}
