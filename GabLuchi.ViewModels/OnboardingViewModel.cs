using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using GabLuchi.Models;
using GabLuchi.Resources;
using GabLuchi.Services;

namespace GabLuchi.ViewModels;

public class OnboardingViewModel : ObservableObject
{
	private readonly CacheService _cache;

	private readonly AuthService _auth;

	private readonly SettingsService _settings;

	private readonly UnlockerService _unlocker;

	private readonly SteamService _steam;

	private readonly PluginInstallerService _installer;

	private readonly ToastService _toast;

	[ObservableProperty]
	private bool _isOpen;

	[ObservableProperty]
	[NotifyPropertyChangedFor("IsGuest")]
	private bool _isSignedIn;

	[ObservableProperty]
	private bool _isSigningIn;

	[ObservableProperty]
	private bool _applyRecommended = true;

	[ObservableProperty]
	private bool _installPlugin = true;

	[ObservableProperty]
	[NotifyPropertyChangedFor("NotBusy")]
	private bool _isBusy;

	[ObservableProperty]
	private string? _statusLine;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand<bool>? setApplyRecommendedCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand<bool>? setInstallPluginCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? signInCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? finishCommand;

	public Func<Task>? RefreshHome { get; set; }

	public bool IsGuest => !IsSignedIn;

	public bool NotBusy => !IsBusy;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsOpen
	{
		get
		{
			return _isOpen;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isOpen, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsOpen);
				_isOpen = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsOpen);
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
	public bool IsSigningIn
	{
		get
		{
			return _isSigningIn;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isSigningIn, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsSigningIn);
				_isSigningIn = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsSigningIn);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ApplyRecommended
	{
		get
		{
			return _applyRecommended;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_applyRecommended, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ApplyRecommended);
				_applyRecommended = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ApplyRecommended);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool InstallPlugin
	{
		get
		{
			return _installPlugin;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_installPlugin, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.InstallPlugin);
				_installPlugin = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.InstallPlugin);
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
				_isBusy = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsBusy);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.NotBusy);
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
	public IRelayCommand<bool> SetApplyRecommendedCommand => setApplyRecommendedCommand ?? (setApplyRecommendedCommand = new RelayCommand<bool>(SetApplyRecommended));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<bool> SetInstallPluginCommand => setInstallPluginCommand ?? (setInstallPluginCommand = new RelayCommand<bool>(SetInstallPlugin));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand SignInCommand => signInCommand ?? (signInCommand = new AsyncRelayCommand(SignIn));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand FinishCommand => finishCommand ?? (finishCommand = new RelayCommand(Finish));

	public OnboardingViewModel(CacheService cache, AuthService auth, SettingsService settings, UnlockerService unlocker, SteamService steam, PluginInstallerService installer, ToastService toast)
	{
		_cache = cache;
		_auth = auth;
		_settings = settings;
		_unlocker = unlocker;
		_steam = steam;
		_installer = installer;
		_toast = toast;
		_auth.AuthStateChanged += delegate
		{
			IsSignedIn = _auth.IsSignedIn;
		};
		IsSignedIn = _auth.IsSignedIn;
	}

	[RelayCommand]
	private void SetApplyRecommended(bool value)
	{
		ApplyRecommended = value;
	}

	[RelayCommand]
	private void SetInstallPlugin(bool value)
	{
		InstallPlugin = value;
	}

	[RelayCommand]
	private async Task SignIn()
	{
		if (IsSigningIn)
		{
			return;
		}
		IsSigningIn = true;
		StatusLine = null;
		try
		{
			await _auth.SignInAsync();
		}
		catch (Exception ex)
		{
			StatusLine = ex.Message;
			IsOpen = true;
		}
		finally
		{
			IsSigningIn = false;
		}
	}

	[RelayCommand]
	private void Finish()
	{
		if (!IsBusy)
		{
			bool applyRecommended = ApplyRecommended;
			bool installPlugin = InstallPlugin;
			_cache.OnboardingComplete = true;
			IsOpen = false;
			if (applyRecommended || installPlugin)
			{
				ApplyChoicesAsync(applyRecommended, installPlugin);
			}
		}
	}

	private async Task ApplyChoicesAsync(bool applyRecommended, bool installPlugin)
	{
		IsBusy = true;
		_toast.Show(Strings.Onboarding_Title, Strings.Onboarding_Applying);
		try
		{
			_ = 3;
			try
			{
				await Task.Run((Action)_steam.StopSteam);
				if (applyRecommended)
				{
					_settings.FastFetch = true;
					ModeInstallResult modeInstallResult = await _unlocker.InstallAsync(UnlockerMode.OpenSteamTools);
					if (!modeInstallResult.Success)
					{
						_toast.Show(Strings.Onboarding_Title, modeInstallResult.Error ?? "", error: true);
					}
				}
				if (installPlugin)
				{
					var (flag, text) = await _installer.InstallAsync(null);
					if (!flag)
					{
						_toast.Show(Strings.Onboarding_Title, text ?? "", error: true);
					}
				}
				await Task.Run((Func<bool>)_steam.StartSteam);
			}
			catch (Exception ex)
			{
				_toast.Show(Strings.Onboarding_Title, ex.Message, error: true);
			}
		}
		finally
		{
			IsBusy = false;
			if (RefreshHome != null)
			{
				try
				{
					await RefreshHome();
				}
				catch
				{
				}
			}
		}
	}
}
