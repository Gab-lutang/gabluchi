using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using GabLuchi;
using GabLuchi.Models;
using GabLuchi.Resources;
using GabLuchi.Services;
using Microsoft.Win32;

namespace GabLuchi.ViewModels;

public class SettingsViewModel : ObservableObject
{
	private readonly SettingsService _settings;

	private readonly SteamService _steam;

	private readonly LicenseService _license;

	private readonly AuthService _auth;

	[ObservableProperty]
	private string? _displayName;

	[ObservableProperty]
	private string? _email;

	[ObservableProperty]
	private string? _avatarUrl;

	[ObservableProperty]
	[NotifyPropertyChangedFor("IsRealUser")]
	private bool _isGuest = true;

	[ObservableProperty]
	private string? _loginRequiredMessage;

	[ObservableProperty]
	[NotifyPropertyChangedFor("ShowBotLinkBanner")]
	private bool _isBotProvisioned;

	[ObservableProperty]
	[NotifyPropertyChangedFor("ShowBotLinkBanner")]
	private bool _botBannerDismissed;

	[ObservableProperty]
	private string _steamPath = "";

	[ObservableProperty]
	private bool _isSteamOverridden;

	[ObservableProperty]
	private string _steamSource = "";

	[ObservableProperty]
	private string? _steamWarning;

	[ObservableProperty]
	private bool _autoUpdateApps;

	[ObservableProperty]
	private bool _fastFetch;

	[ObservableProperty]
	private bool _donateKeys;

	private const string RunKeyPath = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";

	private const string RunValueName = "GabLuchi";

	[ObservableProperty]
	private bool _startWithWindows;

	[ObservableProperty]
	private bool _minimizeToTray;

	[ObservableProperty]
	private LanguageOption _selectedLanguage;

	private bool _suppressLanguagePrompt;

	[ObservableProperty]
	private string _hubcapKeyInput = "";

	[ObservableProperty]
	private string? _hubcapKeyStatus;

	[ObservableProperty]
	private string _hubcapKeyStatusColor = "#22c55e";

	[ObservableProperty]
	[NotifyPropertyChangedFor("ShowHubcapStats")]
	[NotifyPropertyChangedFor("HubcapStatsPending")]
	[NotifyPropertyChangedFor("HubcapStatsText")]
	private bool _hubcapIsKeyConfigured;

	[ObservableProperty]
	[NotifyPropertyChangedFor("CanEditHubcapKey")]
	private bool _isValidatingHubcapKey;

	[ObservableProperty]
	private bool _isRefreshingHubcapStats;

	[ObservableProperty]
	[NotifyPropertyChangedFor("HubcapStatsDisplay")]
	[NotifyPropertyChangedFor("HubcapStatsText")]
	[NotifyPropertyChangedFor("HubcapStatsPending")]
	[NotifyPropertyChangedFor("HubcapUsagePercent")]
	[NotifyPropertyChangedFor("ShowHubcapStats")]
	private HubcapStats? _hubcapStats;

	[ObservableProperty]
	private string _codeInput = "";

	[ObservableProperty]
	[NotifyPropertyChangedFor("CanRedeemCode")]
	private bool _isRedeemingCode;

	[ObservableProperty]
	private string? _codeError;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? dismissLoginRequiredCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? dismissBotBannerCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? signInCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? signInWithCodeCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? overrideSteamFolderCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? clearSteamOverrideCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? openSteamFolderCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? openWebsiteCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? openHubcapCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? signOutCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? refreshHubcapStatsCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? validateAndSaveHubcapKeyCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? clearHubcapKeyCommand;

	public bool IsRealUser => !IsGuest;

	public bool ShowBotLinkBanner => false;

	public Action? RequestShowWindow { get; set; }

	public ObservableCollection<LanguageOption> LanguageOptions { get; } = new ObservableCollection<LanguageOption>
	{
		new LanguageOption(Strings.Settings_Language_SystemDefault, null),
		new LanguageOption("English", "en"),
		new LanguageOption("简体中文", "zh-Hans"),
		new LanguageOption("繁體中文", "zh-Hant"),
		new LanguageOption("日本語", "ja"),
		new LanguageOption("한국어", "ko"),
		new LanguageOption("Español (España)", "es"),
		new LanguageOption("Español (Latinoamérica)", "es-419"),
		new LanguageOption("Português (Brasil)", "pt-BR"),
		new LanguageOption("Português (Portugal)", "pt-PT"),
		new LanguageOption("Français", "fr"),
		new LanguageOption("Deutsch", "de"),
		new LanguageOption("Italiano", "it"),
		new LanguageOption("Nederlands", "nl"),
		new LanguageOption("Polski", "pl"),
		new LanguageOption("Русский", "ru"),
		new LanguageOption("Українська", "uk"),
		new LanguageOption("العربية", "ar"),
		new LanguageOption("Čeština", "cs"),
		new LanguageOption("Magyar", "hu"),
		new LanguageOption("Română", "ro"),
		new LanguageOption("Türkçe", "tr"),
		new LanguageOption("Ελληνικά", "el"),
		new LanguageOption("Български", "bg"),
		new LanguageOption("ไทย", "th"),
		new LanguageOption("Tiếng Việt", "vi"),
		new LanguageOption("Bahasa Indonesia", "id"),
		new LanguageOption("Dansk", "da"),
		new LanguageOption("Suomi", "fi"),
		new LanguageOption("Norsk", "nb"),
		new LanguageOption("Svenska", "sv")
	};

	public bool CanEditHubcapKey => !IsValidatingHubcapKey;

	public string? HubcapStatsDisplay
	{
		get
		{
			if (HubcapStats == null)
			{
				return null;
			}
			return FormatHubcapStats(HubcapStats);
		}
	}

	public double HubcapUsagePercent => 0.0;

	public bool ShowHubcapStats => HubcapIsKeyConfigured;

	public bool HubcapStatsPending => false;

	public string HubcapStatsText => HubcapIsKeyConfigured ? FormatLicenseStatus() : Strings.Common_Loading;

	private string FormatLicenseStatus()
	{
		string? machineId = _license.BoundMachineId;
		string shortMachine = (string.IsNullOrEmpty(machineId) ? "?" : (machineId.Length >= 12 ? machineId.Substring(0, 12) : machineId));
		return string.Format(Strings.Settings_LicenseActive, shortMachine);
	}

	public Func<Task>? RequestSignIn { get; set; }

	public Action? RequestRestart { get; set; }

	public Action? RequestRestartPrompt { get; set; }

	public bool CanRedeemCode => !IsRedeemingCode;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string? DisplayName
	{
		get
		{
			return _displayName;
		}
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_displayName, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.DisplayName);
				_displayName = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.DisplayName);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string? Email
	{
		get
		{
			return _email;
		}
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_email, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Email);
				_email = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Email);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string? AvatarUrl
	{
		get
		{
			return _avatarUrl;
		}
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_avatarUrl, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.AvatarUrl);
				_avatarUrl = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AvatarUrl);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsGuest
	{
		get
		{
			return _isGuest;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isGuest, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsGuest);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsRealUser);
				_isGuest = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsGuest);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsRealUser);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string? LoginRequiredMessage
	{
		get
		{
			return _loginRequiredMessage;
		}
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_loginRequiredMessage, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.LoginRequiredMessage);
				_loginRequiredMessage = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.LoginRequiredMessage);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsBotProvisioned
	{
		get
		{
			return _isBotProvisioned;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isBotProvisioned, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsBotProvisioned);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ShowBotLinkBanner);
				_isBotProvisioned = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsBotProvisioned);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ShowBotLinkBanner);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool BotBannerDismissed
	{
		get
		{
			return _botBannerDismissed;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_botBannerDismissed, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.BotBannerDismissed);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ShowBotLinkBanner);
				_botBannerDismissed = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.BotBannerDismissed);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ShowBotLinkBanner);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string SteamPath
	{
		get
		{
			return _steamPath;
		}
		[MemberNotNull("_steamPath")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_steamPath, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SteamPath);
				_steamPath = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SteamPath);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsSteamOverridden
	{
		get
		{
			return _isSteamOverridden;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isSteamOverridden, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsSteamOverridden);
				_isSteamOverridden = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsSteamOverridden);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string SteamSource
	{
		get
		{
			return _steamSource;
		}
		[MemberNotNull("_steamSource")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_steamSource, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SteamSource);
				_steamSource = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SteamSource);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string? SteamWarning
	{
		get
		{
			return _steamWarning;
		}
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_steamWarning, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SteamWarning);
				_steamWarning = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SteamWarning);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool AutoUpdateApps
	{
		get
		{
			return _autoUpdateApps;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_autoUpdateApps, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.AutoUpdateApps);
				_autoUpdateApps = value;
				OnAutoUpdateAppsChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AutoUpdateApps);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool FastFetch
	{
		get
		{
			return _fastFetch;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_fastFetch, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.FastFetch);
				_fastFetch = value;
				OnFastFetchChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.FastFetch);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool DonateKeys
	{
		get
		{
			return _donateKeys;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_donateKeys, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.DonateKeys);
				_donateKeys = value;
				OnDonateKeysChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.DonateKeys);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool StartWithWindows
	{
		get
		{
			return _startWithWindows;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_startWithWindows, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.StartWithWindows);
				_startWithWindows = value;
				OnStartWithWindowsChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.StartWithWindows);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool MinimizeToTray
	{
		get
		{
			return _minimizeToTray;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_minimizeToTray, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.MinimizeToTray);
				_minimizeToTray = value;
				OnMinimizeToTrayChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.MinimizeToTray);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public LanguageOption SelectedLanguage
	{
		get
		{
			return _selectedLanguage;
		}
		[MemberNotNull("_selectedLanguage")]
		set
		{
			if (!EqualityComparer<LanguageOption>.Default.Equals(_selectedLanguage, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedLanguage);
				_selectedLanguage = value;
				OnSelectedLanguageChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedLanguage);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string HubcapKeyInput
	{
		get
		{
			return _hubcapKeyInput;
		}
		[MemberNotNull("_hubcapKeyInput")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_hubcapKeyInput, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HubcapKeyInput);
				_hubcapKeyInput = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HubcapKeyInput);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string? HubcapKeyStatus
	{
		get
		{
			return _hubcapKeyStatus;
		}
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_hubcapKeyStatus, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HubcapKeyStatus);
				_hubcapKeyStatus = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HubcapKeyStatus);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string HubcapKeyStatusColor
	{
		get
		{
			return _hubcapKeyStatusColor;
		}
		[MemberNotNull("_hubcapKeyStatusColor")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_hubcapKeyStatusColor, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HubcapKeyStatusColor);
				_hubcapKeyStatusColor = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HubcapKeyStatusColor);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool HubcapIsKeyConfigured
	{
		get
		{
			return _hubcapIsKeyConfigured;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_hubcapIsKeyConfigured, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HubcapIsKeyConfigured);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ShowHubcapStats);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HubcapStatsPending);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HubcapStatsText);
				_hubcapIsKeyConfigured = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HubcapIsKeyConfigured);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ShowHubcapStats);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HubcapStatsPending);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HubcapStatsText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsValidatingHubcapKey
	{
		get
		{
			return _isValidatingHubcapKey;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isValidatingHubcapKey, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsValidatingHubcapKey);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CanEditHubcapKey);
				_isValidatingHubcapKey = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsValidatingHubcapKey);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CanEditHubcapKey);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsRefreshingHubcapStats
	{
		get
		{
			return _isRefreshingHubcapStats;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isRefreshingHubcapStats, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsRefreshingHubcapStats);
				_isRefreshingHubcapStats = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsRefreshingHubcapStats);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public HubcapStats? HubcapStats
	{
		get
		{
			return _hubcapStats;
		}
		set
		{
			if (!EqualityComparer<GabLuchi.Models.HubcapStats>.Default.Equals(_hubcapStats, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HubcapStats);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HubcapStatsDisplay);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HubcapStatsText);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HubcapStatsPending);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HubcapUsagePercent);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ShowHubcapStats);
				_hubcapStats = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HubcapStats);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HubcapStatsDisplay);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HubcapStatsText);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HubcapStatsPending);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HubcapUsagePercent);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ShowHubcapStats);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string CodeInput
	{
		get
		{
			return _codeInput;
		}
		[MemberNotNull("_codeInput")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_codeInput, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CodeInput);
				_codeInput = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CodeInput);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsRedeemingCode
	{
		get
		{
			return _isRedeemingCode;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isRedeemingCode, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsRedeemingCode);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CanRedeemCode);
				_isRedeemingCode = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsRedeemingCode);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CanRedeemCode);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string? CodeError
	{
		get
		{
			return _codeError;
		}
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_codeError, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CodeError);
				_codeError = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CodeError);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand DismissLoginRequiredCommand => dismissLoginRequiredCommand ?? (dismissLoginRequiredCommand = new RelayCommand(DismissLoginRequired));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand DismissBotBannerCommand => dismissBotBannerCommand ?? (dismissBotBannerCommand = new RelayCommand(DismissBotBanner));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand SignInCommand => signInCommand ?? (signInCommand = new AsyncRelayCommand(SignInAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand SignInWithCodeCommand => signInWithCodeCommand ?? (signInWithCodeCommand = new AsyncRelayCommand(SignInWithCodeAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand OverrideSteamFolderCommand => overrideSteamFolderCommand ?? (overrideSteamFolderCommand = new RelayCommand(OverrideSteamFolder));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ClearSteamOverrideCommand => clearSteamOverrideCommand ?? (clearSteamOverrideCommand = new RelayCommand(ClearSteamOverride));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand OpenSteamFolderCommand => openSteamFolderCommand ?? (openSteamFolderCommand = new RelayCommand(OpenSteamFolder));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand OpenWebsiteCommand => openWebsiteCommand ?? (openWebsiteCommand = new RelayCommand(OpenWebsite));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand OpenHubcapCommand => openHubcapCommand ?? (openHubcapCommand = new RelayCommand(OpenHubcap));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand SignOutCommand => signOutCommand ?? (signOutCommand = new RelayCommand(SignOut));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand RefreshHubcapStatsCommand => refreshHubcapStatsCommand ?? (refreshHubcapStatsCommand = new AsyncRelayCommand(RefreshHubcapStatsAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ValidateAndSaveHubcapKeyCommand => validateAndSaveHubcapKeyCommand ?? (validateAndSaveHubcapKeyCommand = new AsyncRelayCommand(ValidateAndSaveHubcapKeyAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ClearHubcapKeyCommand => clearHubcapKeyCommand ?? (clearHubcapKeyCommand = new RelayCommand(ClearHubcapKey));

	[RelayCommand]
	private void DismissLoginRequired()
	{
		LoginRequiredMessage = null;
	}

	public SettingsViewModel(SettingsService settings, SteamService steam, LicenseService license, AuthService auth)
	{
		_settings = settings;
		_steam = steam;
		_license = license;
		_auth = auth;
		_auth.AuthStateChanged += RefreshAccount;
		RefreshAccount();
		RefreshSteam();
		_autoUpdateApps = settings.AutoUpdateApps;
		_fastFetch = settings.FastFetch;
		_donateKeys = settings.DonateKeys;
		_startWithWindows = settings.StartWithWindows;
		_minimizeToTray = settings.MinimizeToTray;
		_hubcapIsKeyConfigured = _license.IsActivated;
		_suppressLanguagePrompt = true;
		_selectedLanguage = LanguageOptions.FirstOrDefault((LanguageOption o) => o.Tag == settings.Language) ?? LanguageOptions[0];
		_suppressLanguagePrompt = false;
	}

	private void RefreshSteam()
	{
		string effectivePath = _steam.EffectivePath;
		IsSteamOverridden = _steam.IsOverridden;
		SteamPath = effectivePath ?? Strings.Settings_SteamNotFound;
		SteamSource = (IsSteamOverridden ? Strings.Settings_SteamSource_Custom : ((effectivePath == null) ? Strings.Settings_SteamSource_NotFound : Strings.Settings_SteamSource_Auto));
		SteamWarning = ((effectivePath != null && !_steam.IsValid) ? Strings.Settings_SteamWarning_NoExe : null);
	}

	public void RefreshAccount()
	{
		IsGuest = _auth.IsGuest;
		DisplayName = _auth.DisplayName;
		Email = null;
		AvatarUrl = _auth.AvatarUrl;
		IsBotProvisioned = false;
		LoginRequiredMessage = null;
	}

	[RelayCommand]
	private void DismissBotBanner()
	{
		BotBannerDismissed = true;
	}

	[RelayCommand]
	private async Task SignInAsync()
	{
		if (RequestSignIn != null)
		{
			await RequestSignIn();
		}
	}

	[RelayCommand]
	private async Task SignInWithCodeAsync()
	{
		string text = CodeInput.Trim();
		if (text.Length != 6)
		{
			return;
		}
		IsRedeemingCode = true;
		CodeError = null;
		try
		{
			CodeInput = "";
		}
		catch (Exception ex)
		{
			CodeError = ex.Message;
		}
		finally
		{
			IsRedeemingCode = false;
		}
	}

	[RelayCommand]
	private void OverrideSteamFolder()
	{
		OpenFolderDialog openFolderDialog = new OpenFolderDialog
		{
			Title = Strings.Settings_ChooseSteamFolder,
			InitialDirectory = (_steam.EffectivePath ?? "")
		};
		if (openFolderDialog.ShowDialog() == true)
		{
			_settings.SteamPathOverride = openFolderDialog.FolderName;
			RefreshSteam();
		}
	}

	[RelayCommand]
	private void ClearSteamOverride()
	{
		_settings.SteamPathOverride = null;
		RefreshSteam();
	}

	[RelayCommand]
	private void OpenSteamFolder()
	{
		string effectivePath = _steam.EffectivePath;
		if (effectivePath != null && Directory.Exists(effectivePath))
		{
			Process.Start(new ProcessStartInfo(effectivePath)
			{
				UseShellExecute = true
			});
		}
	}

	[RelayCommand]
	private void OpenWebsite()
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

	[RelayCommand]
	private void OpenHubcap()
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

	[RelayCommand]
	private void SignOut()
	{
		_auth.SignOut();
		RefreshAccount();
	}

	public void OnViewLoaded()
	{
		if (HubcapIsKeyConfigured)
		{
			RefreshHubcapStatsCommand.Execute(null);
		}
		FastFetch = _settings.FastFetch;
	}

	[RelayCommand]
	private async Task RefreshHubcapStatsAsync()
	{
		IsRefreshingHubcapStats = true;
		try
		{
			LicenseAccount? account = await _license.GetAccountAsync();
			if (account == null)
			{
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HubcapStatsText);
				return;
			}
			if (string.Equals(account.Status, "revoked", StringComparison.OrdinalIgnoreCase) || string.Equals(account.Error, "invalid-token", StringComparison.OrdinalIgnoreCase))
			{
				_license.Deactivate();
				HubcapIsKeyConfigured = false;
				HubcapKeyInput = "";
				ShowHubcapStatus(Strings.Settings_LicenseKeyRevoked, isError: true);
				return;
			}
			if (!account.Ok || !account.Buyer)
			{
				_license.Deactivate();
				HubcapIsKeyConfigured = false;
				HubcapKeyInput = "";
				ShowHubcapStatus(Strings.Settings_LicenseKeyError, isError: true);
				return;
			}
			OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HubcapStatsText);
		}
		finally
		{
			IsRefreshingHubcapStats = false;
		}
	}

	[RelayCommand]
	private async Task ValidateAndSaveHubcapKeyAsync()
	{
		string key = HubcapKeyInput.Trim();
		if (!LicenseService.IsValidKeyFormat(key))
		{
			ShowHubcapStatus(Strings.Settings_LicenseKeyBad, isError: true);
			return;
		}
		string? discordId = _auth.UserId;
		if (string.IsNullOrWhiteSpace(discordId))
		{
			ShowHubcapStatus(Strings.Settings_LicenseKeyNeedLogin, isError: true);
			return;
		}
		IsValidatingHubcapKey = true;
		try
		{
			LicenseActivateResult result = await _license.ActivateAsync(key, discordId);
			if (!result.Ok)
			{
				ShowHubcapStatus(ErrorTextFor(result.Error), isError: true);
				return;
			}
			HubcapIsKeyConfigured = true;
			HubcapKeyInput = "";
			HubcapKeyStatus = null;
		}
		finally
		{
			IsValidatingHubcapKey = false;
		}
	}

	private string ErrorTextFor(string? error)
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
			default:
				return Strings.Settings_LicenseKeyError;
		}
	}

	[RelayCommand]
	private void ClearHubcapKey()
	{
		_license.Deactivate();
		HubcapIsKeyConfigured = false;
		HubcapKeyInput = "";
		HubcapKeyStatus = null;
		HubcapStats = null;
	}

	private void ShowHubcapStatus(string text, bool isError)
	{
		HubcapKeyStatus = text;
		HubcapKeyStatusColor = (isError ? "#f87171" : "#22c55e");
	}

	private static string FormatHubcapStats(HubcapStats stats)
	{
		string result = string.Format(Strings.Settings_HubcapKeyOk, stats.DailyUsage, stats.DailyLimit);
		if (DateTimeOffset.TryParse(stats.ApiKeyExpiresAt, out var result2))
		{
			return string.Format(Strings.Settings_HubcapKeyOkExpiry, stats.DailyUsage, stats.DailyLimit, result2.ToString("yyyy-MM-dd"));
		}
		return result;
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnAutoUpdateAppsChanged(bool value)
	{
		_settings.AutoUpdateApps = value;
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnFastFetchChanged(bool value)
	{
		_settings.FastFetch = value;
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnDonateKeysChanged(bool value)
	{
		_settings.DonateKeys = value;
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnStartWithWindowsChanged(bool value)
	{
		_settings.StartWithWindows = value;
		try
		{
			using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true);
			if (registryKey != null)
			{
				if (value)
				{
					registryKey.SetValue("GabLuchi", "\"" + Environment.ProcessPath + "\" --minimized");
				}
				else
				{
					registryKey.DeleteValue("GabLuchi", throwOnMissingValue: false);
				}
			}
		}
		catch
		{
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnMinimizeToTrayChanged(bool value)
	{
		_settings.MinimizeToTray = value;
		if (!value)
		{
			RequestShowWindow?.Invoke();
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnSelectedLanguageChanged(LanguageOption value)
	{
		if ((object)value != null && !_suppressLanguagePrompt)
		{
			_settings.Language = value.Tag;
			RequestRestartPrompt?.Invoke();
		}
	}
}
