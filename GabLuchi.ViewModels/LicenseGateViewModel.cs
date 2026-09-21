using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using GabLuchi.Models;
using GabLuchi.Resources;
using GabLuchi.Services;

namespace GabLuchi.ViewModels;

public class LicenseGateViewModel : ObservableObject
{
	private readonly LicenseService _license;

	private readonly AuthService _auth;

	private readonly SettingsService _settings;

	private readonly UsageService _usage;

	[ObservableProperty]
	private bool _isOpen;

	[ObservableProperty]
	[NotifyPropertyChangedFor("IsGuest")]
	private bool _isSignedIn;

	[ObservableProperty]
	private bool _isSigningIn;

	[ObservableProperty]
	[NotifyPropertyChangedFor("NotBusy")]
	private bool _isBusy;

	[ObservableProperty]
	private string? _statusLine;

	[ObservableProperty]
	private string? _statusColor;

	[ObservableProperty]
	private string _keyInput = "";

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? signInCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? activateCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? openDiscordCommand;

	public bool IsGuest => !IsSignedIn;

	public bool NotBusy => !IsBusy;

	public bool IsActivated => _license.IsActivated;

	public Func<Task>? PostActivationRefresh { get; set; }

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

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string? StatusColor
	{
		get
		{
			return _statusColor;
		}
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_statusColor, value))
			{
				OnPropertyChanging("StatusColor");
				_statusColor = value;
				OnPropertyChanged("StatusColor");
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string KeyInput
	{
		get
		{
			return _keyInput;
		}
		[MemberNotNull("_keyInput")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_keyInput, value))
			{
				OnPropertyChanging("KeyInput");
				_keyInput = value;
				OnPropertyChanged("KeyInput");
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand SignInCommand => signInCommand ?? (signInCommand = new AsyncRelayCommand(SignIn));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ActivateCommand => activateCommand ?? (activateCommand = new AsyncRelayCommand(Activate));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand OpenDiscordCommand => openDiscordCommand ?? (openDiscordCommand = new RelayCommand(OpenDiscord));

	public LicenseGateViewModel(LicenseService license, AuthService auth, SettingsService settings, UsageService usage)
	{
		_license = license;
		_auth = auth;
		_settings = settings;
		_usage = usage;
		_auth.AuthStateChanged += delegate
		{
			IsSignedIn = _auth.IsSignedIn;
		};
		IsSignedIn = _auth.IsSignedIn;
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
			StatusColor = "#f87171";
		}
		finally
		{
			IsSigningIn = false;
		}
	}

	[RelayCommand]
	private async Task Activate()
	{
		string key = KeyInput.Trim();
		if (!LicenseService.IsValidKeyFormat(key))
		{
			ShowStatus(Strings.Settings_LicenseKeyBad, true);
			return;
		}
		string? discordId = _auth.UserId;
		if (string.IsNullOrWhiteSpace(discordId))
		{
			ShowStatus(Strings.Settings_LicenseKeyNeedLogin, true);
			return;
		}
		IsBusy = true;
		try
		{
			LicenseActivateResult result = await _license.ActivateAsync(key, discordId);
			if (!result.Ok)
			{
				ShowStatus(ErrorTextFor(result.Error), true);
				return;
			}
			KeyInput = "";
			_usage.SetTier(result.Tier, result.ExpiresAt);
			IsOpen = false;
			OnPropertyChanged(nameof(IsActivated));
			if (PostActivationRefresh != null)
			{
				try { await PostActivationRefresh(); }
				catch { }
			}
		}
		finally
		{
			IsBusy = false;
		}
	}

	[RelayCommand]
	private void OpenDiscord()
	{
		string url = Config.DiscordInviteUrl;
		if (!string.IsNullOrWhiteSpace(url))
		{
			Process.Start(new ProcessStartInfo(url)
			{
				UseShellExecute = true
			});
		}
	}

	private void ShowStatus(string text, bool isError)
	{
		StatusLine = text;
		StatusColor = isError ? "#f87171" : "#22c55e";
	}

	private static string ErrorTextFor(string? error)
	{
		switch (error)
		{
			case "invalid-key":
				return Strings.Settings_LicenseKeyInvalid;
			case "already-in-use":
				return Strings.Settings_LicenseKeyUsed;
			case "owner-mismatch":
				return Strings.Settings_LicenseKeyOwnerMismatch;
			case "revoked":
				return Strings.Settings_LicenseKeyRevoked;
			case "unreachable":
				return Strings.Settings_LicenseKeyUnreachable;
			case "free-key-expired":
				return "Your free key has expired. Run /freekey on Discord for a new one.";
			case "alt-detected":
				return "Alt detected — this machine was used by another account. Contact support.";
			default:
				return Strings.Settings_LicenseKeyError;
		}
	}
}
