using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using GabLuchi.Models;
using GabLuchi.Resources;
using GabLuchi.Services;

namespace GabLuchi.ViewModels;

public class DownloadViewModel : ObservableObject
{
	private readonly GabLuchiApiClient _api;

	private readonly HubcapService _hubcap;

	private readonly SettingsService _settings;

	private readonly ManifestDownloader _manifestDownloader;

	private readonly ToastService _toast;

	private readonly LuaInstaller _installer;

	private readonly SteamAppListCache _appList;

	private readonly SteamAppInfoCache _appInfo;

	private readonly SteamDepotInfo _depotInfo;

	private readonly HardwareAppIdService _hardware;

	private CancellationTokenSource? _searchCts;

	private CancellationTokenSource? _detailsCts;

	private IReadOnlyDictionary<long, ContentDepot> _depotsById = new Dictionary<long, ContentDepot>();

	[ObservableProperty]
	[NotifyPropertyChangedFor("ShowFeatured")]
	private string _searchText = "";

	[ObservableProperty]
	private bool _isSearching;

	[ObservableProperty]
	private bool _isResultsOpen;

	[ObservableProperty]
	[NotifyPropertyChangedFor("HasDetails")]
	[NotifyPropertyChangedFor("ShowFeatured")]
	[NotifyCanExecuteChangedFor("FetchCommand")]
	private GameDetails? _details;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor("FetchCommand")]
	private bool _isChecking;

	[ObservableProperty]
	[NotifyPropertyChangedFor("HasSources")]
	[NotifyPropertyChangedFor("ShowFeatured")]
	private bool _sourcesLoaded;

	[ObservableProperty]
	[NotifyPropertyChangedFor("HasDlcInfo")]
	[NotifyPropertyChangedFor("DlcComplete")]
	[NotifyPropertyChangedFor("CanGenerateDlc")]
	private DlcInfo? _dlcInfo;

	[ObservableProperty]
	private bool _isGenerating;

	[ObservableProperty]
	private double _generateProgress;

	[ObservableProperty]
	private string? _error;

	[ObservableProperty]
	[NotifyPropertyChangedFor("HasLastDownload")]
	private DownloadedFile? _lastDownload;

	private bool _silentInstall;

	[ObservableProperty]
	[NotifyPropertyChangedFor("HasInstallResult")]
	[NotifyPropertyChangedFor("ShowFeatured")]
	private string? _installStatus;

	[ObservableProperty]
	private bool _installFailed;

	private long? _installedAppId;

	[ObservableProperty]
	[NotifyPropertyChangedFor("HasDiffAdded")]
	private List<DiffRow> _diffAdded = new List<DiffRow>();

	[ObservableProperty]
	[NotifyPropertyChangedFor("HasDiffRemoved")]
	private List<DiffRow> _diffRemoved = new List<DiffRow>();

	[ObservableProperty]
	private bool _isConfirmingOverwrite;

	[ObservableProperty]
	private string _confirmTitle = "";

	[ObservableProperty]
	private string? _confirmNoChanges;

	private (string zipPath, long appId)? _pendingInstall;

	private bool _suppressSearch;

	private string? _fastFetchSource;

	[ObservableProperty]
	private bool _fastFetch;

	private const string HubcapSourceName = "Sadie (Morrenus)";

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand<SteamSearchResult>? selectResultCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand<FeaturedItem>? selectFeaturedCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? closeResultsCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? fetchCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? generateDlcCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? confirmOverwriteCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? cancelOverwriteCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand<DiffRow>? openSteamDbCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? revealInstalledCommand;

	public Action<long>? NavigateToGame { get; set; }

	public ObservableCollection<SteamSearchResult> SearchResults { get; } = new ObservableCollection<SteamSearchResult>();

	public ObservableCollection<SourceRowViewModel> Sources { get; } = new ObservableCollection<SourceRowViewModel>();

	public ObservableCollection<DlcDepot> DlcDepots { get; } = new ObservableCollection<DlcDepot>();

	public ObservableCollection<FeaturedItem> TopSellers { get; } = new ObservableCollection<FeaturedItem>();

	public ObservableCollection<FeaturedItem> NewReleases { get; } = new ObservableCollection<FeaturedItem>();

	public bool HasTopSellers => TopSellers.Count > 0;

	public bool HasNewReleases => NewReleases.Count > 0;

	public bool ShowFeatured
	{
		get
		{
			if ((TopSellers.Count > 0 || NewReleases.Count > 0) && string.IsNullOrWhiteSpace(SearchText) && !HasDetails && !HasSources)
			{
				return !HasInstallResult;
			}
			return false;
		}
	}

	public bool HasDetails => Details != null;

	public string GenresText
	{
		get
		{
			if (Details != null)
			{
				return string.Join(", ", Details.Genres);
			}
			return "";
		}
	}

	public bool HasSources
	{
		get
		{
			if (SourcesLoaded)
			{
				return Sources.Count > 0;
			}
			return false;
		}
	}

	public bool HasDlcInfo => DlcInfo != null;

	public bool DlcComplete
	{
		get
		{
			DlcInfo? dlcInfo = DlcInfo;
			if (dlcInfo == null)
			{
				return false;
			}
			return dlcInfo.MissingCount == 0;
		}
	}

	public bool CanGenerateDlc
	{
		get
		{
			if (DlcInfo != null)
			{
				if (DlcInfo.HaveCount <= 0)
				{
					return DlcInfo.MissingCount == 0;
				}
				return true;
			}
			return false;
		}
	}

	public bool HasLastDownload => (object)LastDownload != null;

	public DropInstallViewModel Drop { get; }

	public bool HasInstallResult => InstallStatus != null;

	public bool HasDiffAdded => DiffAdded.Count > 0;

	public bool HasDiffRemoved => DiffRemoved.Count > 0;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string SearchText
	{
		get
		{
			return _searchText;
		}
		[MemberNotNull("_searchText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_searchText, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SearchText);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ShowFeatured);
				_searchText = value;
				OnSearchTextChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SearchText);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ShowFeatured);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsSearching
	{
		get
		{
			return _isSearching;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isSearching, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsSearching);
				_isSearching = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsSearching);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsResultsOpen
	{
		get
		{
			return _isResultsOpen;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isResultsOpen, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsResultsOpen);
				_isResultsOpen = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsResultsOpen);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public GameDetails? Details
	{
		get
		{
			return _details;
		}
		set
		{
			if (!EqualityComparer<GameDetails>.Default.Equals(_details, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Details);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HasDetails);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ShowFeatured);
				_details = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Details);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HasDetails);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ShowFeatured);
				FetchCommand.NotifyCanExecuteChanged();
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsChecking
	{
		get
		{
			return _isChecking;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isChecking, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsChecking);
				_isChecking = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsChecking);
				FetchCommand.NotifyCanExecuteChanged();
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool SourcesLoaded
	{
		get
		{
			return _sourcesLoaded;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_sourcesLoaded, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SourcesLoaded);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HasSources);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ShowFeatured);
				_sourcesLoaded = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SourcesLoaded);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HasSources);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ShowFeatured);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public DlcInfo? DlcInfo
	{
		get
		{
			return _dlcInfo;
		}
		set
		{
			if (!EqualityComparer<GabLuchi.Models.DlcInfo>.Default.Equals(_dlcInfo, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.DlcInfo);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HasDlcInfo);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.DlcComplete);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CanGenerateDlc);
				_dlcInfo = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.DlcInfo);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HasDlcInfo);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.DlcComplete);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CanGenerateDlc);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsGenerating
	{
		get
		{
			return _isGenerating;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isGenerating, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsGenerating);
				_isGenerating = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsGenerating);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public double GenerateProgress
	{
		get
		{
			return _generateProgress;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(_generateProgress, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.GenerateProgress);
				_generateProgress = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.GenerateProgress);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string? Error
	{
		get
		{
			return _error;
		}
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_error, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Error);
				_error = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Error);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public DownloadedFile? LastDownload
	{
		get
		{
			return _lastDownload;
		}
		set
		{
			if (!EqualityComparer<DownloadedFile>.Default.Equals(_lastDownload, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.LastDownload);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HasLastDownload);
				_lastDownload = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.LastDownload);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HasLastDownload);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string? InstallStatus
	{
		get
		{
			return _installStatus;
		}
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_installStatus, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.InstallStatus);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HasInstallResult);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ShowFeatured);
				_installStatus = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.InstallStatus);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HasInstallResult);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ShowFeatured);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool InstallFailed
	{
		get
		{
			return _installFailed;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_installFailed, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.InstallFailed);
				_installFailed = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.InstallFailed);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public List<DiffRow> DiffAdded
	{
		get
		{
			return _diffAdded;
		}
		[MemberNotNull("_diffAdded")]
		set
		{
			if (!EqualityComparer<List<DiffRow>>.Default.Equals(_diffAdded, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.DiffAdded);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HasDiffAdded);
				_diffAdded = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.DiffAdded);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HasDiffAdded);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public List<DiffRow> DiffRemoved
	{
		get
		{
			return _diffRemoved;
		}
		[MemberNotNull("_diffRemoved")]
		set
		{
			if (!EqualityComparer<List<DiffRow>>.Default.Equals(_diffRemoved, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.DiffRemoved);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HasDiffRemoved);
				_diffRemoved = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.DiffRemoved);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HasDiffRemoved);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsConfirmingOverwrite
	{
		get
		{
			return _isConfirmingOverwrite;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isConfirmingOverwrite, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsConfirmingOverwrite);
				_isConfirmingOverwrite = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsConfirmingOverwrite);
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
	public string? ConfirmNoChanges
	{
		get
		{
			return _confirmNoChanges;
		}
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_confirmNoChanges, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ConfirmNoChanges);
				_confirmNoChanges = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ConfirmNoChanges);
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

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<SteamSearchResult> SelectResultCommand => selectResultCommand ?? (selectResultCommand = new AsyncRelayCommand<SteamSearchResult>(SelectResultAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<FeaturedItem> SelectFeaturedCommand => selectFeaturedCommand ?? (selectFeaturedCommand = new AsyncRelayCommand<FeaturedItem>(SelectFeaturedAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand CloseResultsCommand => closeResultsCommand ?? (closeResultsCommand = new RelayCommand(CloseResults));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand FetchCommand => fetchCommand ?? (fetchCommand = new AsyncRelayCommand(FetchAsync, CanFetch));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand GenerateDlcCommand => generateDlcCommand ?? (generateDlcCommand = new AsyncRelayCommand(GenerateDlcAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ConfirmOverwriteCommand => confirmOverwriteCommand ?? (confirmOverwriteCommand = new RelayCommand(ConfirmOverwrite));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand CancelOverwriteCommand => cancelOverwriteCommand ?? (cancelOverwriteCommand = new RelayCommand(CancelOverwrite));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<DiffRow> OpenSteamDbCommand => openSteamDbCommand ?? (openSteamDbCommand = new RelayCommand<DiffRow>(OpenSteamDb));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand RevealInstalledCommand => revealInstalledCommand ?? (revealInstalledCommand = new RelayCommand(RevealInstalled));

	public async Task ProtocolInstall(long appId, Action<string, bool>? onComplete = null)
	{
		_silentInstall = onComplete != null;
		FastFetch = true;
		_suppressSearch = true;
		SearchText = appId.ToString();
		_suppressSearch = false;
		ResetResults();
		IsResultsOpen = false;
		try
		{
			Details = await _api.GetDetailsAsync(appId.ToString());
			OnPropertyChanged("GenresText");
		}
		catch
		{
			onComplete?.Invoke(Strings.Add_Err_Generic, arg2: true);
			_silentInstall = false;
			return;
		}
		if (HasDetails)
		{
			await FetchCommand.ExecuteAsync(null);
		}
		if (onComplete != null)
		{
			if (InstallStatus != null)
			{
				onComplete(InstallStatus, InstallFailed);
			}
			else
			{
				onComplete(Error ?? Strings.Add_Err_Generic, arg2: true);
			}
			_silentInstall = false;
		}
	}

	public async Task StartPluginAddAsync(long appId)
	{
		_silentInstall = true;
		_suppressSearch = true;
		SearchText = appId.ToString();
		_suppressSearch = false;
		ResetResults();
		IsResultsOpen = false;
		InstallStatus = null;
		Error = null;
		try
		{
			Details = await _api.GetDetailsAsync(appId.ToString());
			OnPropertyChanged("GenresText");
		}
		catch
		{
			Error = Strings.Add_Err_Generic;
			return;
		}
		if (HasDetails)
		{
			await FetchCommand.ExecuteAsync(null);
		}
	}

	public Task DownloadSourceByNameAsync(string name)
	{
		_silentInstall = true;
		SourceRowViewModel sourceRowViewModel = Sources.FirstOrDefault((SourceRowViewModel s) => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase) || string.Equals(s.DisplayName, name, StringComparison.OrdinalIgnoreCase));
		if (sourceRowViewModel != null)
		{
			return DownloadFromSourceAsync(sourceRowViewModel);
		}
		return Task.CompletedTask;
	}

	public void SyncFastFetch()
	{
		FastFetch = _settings.FastFetch;
	}

	public DownloadViewModel(GabLuchiApiClient api, HubcapService hubcap, SettingsService settings, ManifestDownloader manifestDownloader, ToastService toast, LuaInstaller installer, SteamAppListCache appList, SteamAppInfoCache appInfo, SteamDepotInfo depotInfo, HardwareAppIdService hardware, DropInstallViewModel drop)
	{
		_api = api;
		_hubcap = hubcap;
		_settings = settings;
		_manifestDownloader = manifestDownloader;
		_toast = toast;
		_installer = installer;
		_appList = appList;
		_appInfo = appInfo;
		_depotInfo = depotInfo;
		_hardware = hardware;
		Drop = drop;
		_fastFetch = settings.FastFetch;
	}

	public void SeedSearch(long appId)
	{
		_suppressSearch = true;
		SearchText = appId.ToString();
		_suppressSearch = false;
		ResetResults();
		Details = null;
		IsResultsOpen = false;
		FetchDetailsDebouncedAsync(appId.ToString());
	}


	private async Task SearchDebouncedAsync(string query)
	{
		_searchCts?.Cancel();
		CancellationTokenSource cts = (_searchCts = new CancellationTokenSource());
		try
		{
			await Task.Delay(350, cts.Token);
			if (string.IsNullOrWhiteSpace(query) || query.Length > 100)
			{
				SearchResults.Clear();
				IsResultsOpen = false;
				return;
			}
			IsSearching = true;
			List<SteamSearchResult> list = await _api.SearchAsync(query.Trim(), cts.Token);
			if (cts.Token.IsCancellationRequested)
			{
				return;
			}
			SearchResults.Clear();
			foreach (SteamSearchResult item in list)
			{
				if (!_hardware.IsBlacklisted(item.AppId))
				{
					SearchResults.Add(item);
				}
			}
			IsResultsOpen = SearchResults.Count > 0;
		}
		catch (OperationCanceledException)
		{
		}
		catch (ApiException ex2)
		{
			Error = ex2.Message;
		}
		catch
		{
		}
		finally
		{
			if (_searchCts == cts)
			{
				IsSearching = false;
			}
		}
	}

	private async Task FetchDetailsDebouncedAsync(string appid)
	{
		_detailsCts?.Cancel();
		CancellationTokenSource cts = (_detailsCts = new CancellationTokenSource());
		try
		{
			await Task.Delay(400, cts.Token);
			Details = await _api.GetDetailsAsync(appid, cts.Token);
			OnPropertyChanged("GenresText");
		}
		catch (OperationCanceledException)
		{
		}
		catch
		{
			Details = null;
		}
	}

	[RelayCommand]
	private async Task SelectResultAsync(SteamSearchResult result)
	{
		_suppressSearch = true;
		SearchText = result.Name;
		_suppressSearch = false;
		IsResultsOpen = false;
		ResetResults();
		_detailsCts?.Cancel();
		CancellationTokenSource cancellationTokenSource = (_detailsCts = new CancellationTokenSource());
		try
		{
			Details = await _api.GetDetailsAsync(result.AppId.ToString(), cancellationTokenSource.Token);
			OnPropertyChanged("GenresText");
		}
		catch (OperationCanceledException)
		{
		}
		catch
		{
			Details = null;
		}
	}

	[RelayCommand]
	private async Task SelectFeaturedAsync(FeaturedItem item)
	{
		if ((object)item == null)
		{
			return;
		}
		_suppressSearch = true;
		SearchText = item.Name;
		_suppressSearch = false;
		IsResultsOpen = false;
		ResetResults();
		_detailsCts?.Cancel();
		CancellationTokenSource cancellationTokenSource = (_detailsCts = new CancellationTokenSource());
		try
		{
			Details = await _api.GetDetailsAsync(item.AppId.ToString(), cancellationTokenSource.Token);
			OnPropertyChanged("GenresText");
		}
		catch (OperationCanceledException)
		{
		}
		catch
		{
			Details = null;
		}
	}

	public async Task LoadFeaturedAsync()
	{
		if (TopSellers.Count > 0 || NewReleases.Count > 0)
		{
			return;
		}
		await _hardware.EnsureFreshAsync();
		var (list, list2) = await _api.GetFeaturedAsync();
		foreach (SteamFeaturedItem item in list)
		{
			if (!_hardware.IsBlacklisted(item.Id))
			{
				TopSellers.Add(new FeaturedItem(item.Id, item.Name, item.LargeCapsuleImage));
			}
		}
		foreach (SteamFeaturedItem item2 in list2)
		{
			if (!_hardware.IsBlacklisted(item2.Id))
			{
				NewReleases.Add(new FeaturedItem(item2.Id, item2.Name, item2.LargeCapsuleImage));
			}
		}
		OnPropertyChanged("HasTopSellers");
		OnPropertyChanged("HasNewReleases");
		OnPropertyChanged("ShowFeatured");
	}

	[RelayCommand]
	private void CloseResults()
	{
		IsResultsOpen = false;
	}

	private bool CanFetch()
	{
		if (HasDetails)
		{
			return !IsChecking;
		}
		return false;
	}

	[RelayCommand(CanExecute = "CanFetch")]
	private async Task FetchAsync()
	{
		if (Details == null)
		{
			return;
		}
		ResetResults();
		IsChecking = true;
		try
		{
			if (Details.IsDlc)
			{
				if (Details.BaseAppId == null)
				{
					Error = Strings.Add_Err_BaseGame;
					return;
				}
				DlcInfo = await _api.GetDlcInfoAsync(Details.AppId.ToString(), Details.BaseAppId);
				DlcDepots.Clear();
				IEnumerable<DlcDepot> enumerable = DlcInfo?.Depots.OrderByDescending((DlcDepot d) => d.Included);
				{
					foreach (DlcDepot item in enumerable ?? Enumerable.Empty<DlcDepot>())
					{
						DlcDepots.Add(item);
					}
					return;
				}
			}
			Dictionary<string, string> statuses = await _api.CheckSourcesAsync(Details.AppId.ToString());
			await AddHubcapSourceAsync(statuses, Details.AppId.ToString());
			foreach (var (name, status) in statuses.OrderByDescending<KeyValuePair<string, string>, int>((KeyValuePair<string, string> kv) => SourceMeta.Get(kv.Key).RequiresUserKey ? 1 : 0))
			{
				Sources.Add(new SourceRowViewModel(this, name, status));
			}
			await ApplyHubcapStateAsync();
			if (FastFetch)
			{
				SourceRowViewModel sourceRowViewModel = Sources.FirstOrDefault((SourceRowViewModel s) => s.CanDownload);
				if (sourceRowViewModel == null)
				{
					Error = Strings.Add_FastFetch_NoSource;
					return;
				}
				_fastFetchSource = sourceRowViewModel.DisplayName;
				await DownloadFromSourceAsync(sourceRowViewModel);
			}
			else
			{
				SourcesLoaded = true;
			}
		}
		catch (ApiException ex)
		{
			Error = ex.Message;
		}
		catch (Exception)
		{
			Error = Strings.Add_Err_Generic;
		}
		finally
		{
			IsChecking = false;
		}
	}

	private async Task AddHubcapSourceAsync(Dictionary<string, string> statuses, string appid)
	{
		string hubcapApiKey = _settings.HubcapApiKey;
		if (string.IsNullOrEmpty(hubcapApiKey))
		{
			statuses["Sadie (Morrenus)"] = "unknown";
			return;
		}
		HubcapManifestStatus hubcapManifestStatus = await _hubcap.CheckStatusAsync(hubcapApiKey, appid);
		statuses["Sadie (Morrenus)"] = ((hubcapManifestStatus != null && hubcapManifestStatus.ManifestFileExists) ? "available" : "unavailable");
	}

	private async Task ApplyHubcapStateAsync()
	{
		List<SourceRowViewModel> keyRows = Sources.Where((SourceRowViewModel s) => s.NeedsKey).ToList();
		if (keyRows.Count == 0)
		{
			return;
		}
		string hubcapApiKey = _settings.HubcapApiKey;
		if (string.IsNullOrEmpty(hubcapApiKey))
		{
			foreach (SourceRowViewModel item in keyRows)
			{
				item.IsLocked = true;
			}
			return;
		}
		HubcapStats hubcapStats = await _hubcap.GetStatsAsync(hubcapApiKey);
		foreach (SourceRowViewModel item2 in keyRows)
		{
			item2.IsLocked = hubcapStats == null || !hubcapStats.CanMakeRequests;
			if (hubcapStats != null)
			{
				item2.StatsText = $"{hubcapStats.DailyUsage}/{hubcapStats.DailyLimit}";
			}
		}
	}


	public async Task DownloadFromSourceAsync(SourceRowViewModel source)
	{
		if (Details == null || Sources.Any((SourceRowViewModel s) => s.IsDownloading))
		{
			return;
		}
		Error = null;
		LastDownload = null;
		InstallStatus = null;
		long appId = Details.AppId;
		source.IsDownloading = true;
		source.IsProgressIndeterminate = true;
		try
		{
			Progress<double?> progress = new Progress<double?>(delegate(double? p)
			{
				source.IsProgressIndeterminate = !p.HasValue;
				if (p.HasValue)
				{
					source.Progress = p.Value * 100.0;
				}
			});
			DownloadedFile download = (LastDownload = ((!source.NeedsKey) ? (await _manifestDownloader.DownloadManifestAsync(appId.ToString(), source.Name, Details.Name, progress)) : (await _hubcap.DownloadManifestAsync(appId.ToString(), _settings.HubcapApiKey ?? "", progress))));
			if (!source.NeedsKey)
			{
			}
			else
			{
				await ApplyHubcapStateAsync();
			}
			string text = _installer.ReadInstalledLua(appId);
			bool flag = !_silentInstall && text != null;
			if (flag)
			{
				flag = await ShowOverwriteConfirmAsync(text, download.FilePath, appId);
			}
			if (!flag)
			{
				InstallZipAndReport(download.FilePath, appId);
			}
		}
		catch (ApiException ex)
		{
			Error = ex.Message;
		}
		catch (Exception)
		{
			Error = Strings.Add_Err_Download;
		}
		finally
		{
			source.IsDownloading = false;
			source.Progress = 0.0;
			source.IsProgressIndeterminate = false;
		}
	}

	[RelayCommand]
	private async Task GenerateDlcAsync()
	{
		if (Details?.BaseAppId == null || IsGenerating)
		{
			return;
		}
		Error = null;
		LastDownload = null;
		InstallStatus = null;
		long appId = Details.AppId;
		IsGenerating = true;
		try
		{
			DownloadedFile downloadedFile = (LastDownload = await _api.GenerateDlcAsync(appId.ToString(), Details.BaseAppId, Details.Name, null));
			InstallResult result = _installer.InstallLua(downloadedFile.FilePath, appId);
			ReportInstall(result);
			DeleteStaged(downloadedFile.FilePath);
		}
		catch (ApiException ex)
		{
			Error = ex.Message;
		}
		catch (Exception)
		{
			Error = Strings.Add_Err_Generate;
		}
		finally
		{
			IsGenerating = false;
		}
	}

	private async Task<bool> ShowOverwriteConfirmAsync(string oldLuaPath, string newZipPath, long appId)
	{
		LuaContents oldLua = LuaFileParser.Parse(oldLuaPath, appId);
		LuaContents luaContents = ExtractLuaFromZip(newZipPath, appId);
		if ((object)luaContents == null)
		{
			InstallZipAndReport(newZipPath, appId);
			return false;
		}
		LuaDiff diff = LuaFileParser.Diff(oldLua, luaContents);
		await _appList.EnsureLoadedAsync();
		_depotsById = await BuildDepotLookupAsync(appId);
		DiffAdded = diff.Added.Select(ToDiffRow).ToList();
		DiffRemoved = diff.Removed.Select(ToDiffRow).ToList();
		ConfirmTitle = string.Format(Strings.Add_Confirm_Replace, Details?.Name ?? appId.ToString());
		ConfirmNoChanges = (diff.HasChanges ? null : Strings.Add_Confirm_NoChanges);
		_pendingInstall = (newZipPath, appId);
		IsConfirmingOverwrite = true;
		List<long> list = (from e in diff.Added.Concat(diff.Removed)
			where DiffDisplayName(e) == null
			select e.Id).Distinct().ToList();
		if (list.Count > 0)
		{
			await Parallel.ForEachAsync(list, new ParallelOptions
			{
				MaxDegreeOfParallelism = 4
			}, async delegate(long id, CancellationToken _)
			{
				await _appInfo.ResolveAsync(id);
			});
			if (IsConfirmingOverwrite)
			{
				(string, long)? pendingInstall = _pendingInstall;
				if (pendingInstall.HasValue && pendingInstall.GetValueOrDefault().Item2 == appId)
				{
					DiffAdded = diff.Added.Select(ToDiffRow).ToList();
					DiffRemoved = diff.Removed.Select(ToDiffRow).ToList();
				}
			}
		}
		return true;
	}

	[RelayCommand]
	private void ConfirmOverwrite()
	{
		IsConfirmingOverwrite = false;
		(string, long)? pendingInstall = _pendingInstall;
		if (pendingInstall.HasValue)
		{
			(string, long) valueOrDefault = pendingInstall.GetValueOrDefault();
			InstallZipAndReport(valueOrDefault.Item1, valueOrDefault.Item2);
		}
		_pendingInstall = null;
	}

	[RelayCommand]
	private void CancelOverwrite()
	{
		IsConfirmingOverwrite = false;
		(string, long)? pendingInstall = _pendingInstall;
		if (pendingInstall.HasValue)
		{
			DeleteStaged(pendingInstall.GetValueOrDefault().Item1);
		}
		_pendingInstall = null;
		InstallStatus = Strings.Add_Status_Cancelled;
		InstallFailed = false;
	}

	private void InstallZipAndReport(string zipPath, long appId)
	{
		_installedAppId = appId;
		ReportInstall(IsZip(zipPath) ? _installer.InstallZip(zipPath, appId) : _installer.InstallLua(zipPath, appId));
		DeleteStaged(zipPath);
	}

	private static void DeleteStaged(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch
		{
		}
	}

	private static bool IsZip(string path)
	{
		try
		{
			using FileStream fileStream = File.OpenRead(path);
			Span<byte> buffer = stackalloc byte[4];
			return fileStream.Read(buffer) == 4 && buffer[0] == 80 && buffer[1] == 75 && buffer[2] == 3 && buffer[3] == 4;
		}
		catch
		{
			return false;
		}
	}

	private void ReportInstall(InstallResult result)
	{
		if (result.Error != null)
		{
			InstallFailed = true;
			InstallStatus = result.Error;
			return;
		}
		if (result.AnyFailed)
		{
			InstallFailed = true;
			InstallStatus = string.Format(Strings.Add_Status_InstallFailed, result.Failed.Count);
			return;
		}
		InstallFailed = false;
		string arg = Details?.Name ?? "lua";
		InstallStatus = ((result.ManifestCount > 0) ? string.Format(Strings.Add_Status_AddedManifests, arg, result.ManifestCount) : string.Format(Strings.Add_Status_AddedFetch, arg));
		if (_fastFetchSource != null)
		{
			InstallStatus = InstallStatus + " " + string.Format(Strings.Add_FastFetch_Via, _fastFetchSource);
			_fastFetchSource = null;
		}
	}

	private async Task<IReadOnlyDictionary<long, ContentDepot>> BuildDepotLookupAsync(long appId)
	{
		Dictionary<long, ContentDepot> map = new Dictionary<long, ContentDepot>();
		AppDepotInfo appDepotInfo = await _depotInfo.GetAsync(appId);
		if ((object)appDepotInfo == null)
		{
			return map;
		}
		foreach (ContentDepot depot in appDepotInfo.Depots)
		{
			map[depot.Id] = depot;
			long? dlcAppId = depot.DlcAppId;
			if (dlcAppId.HasValue)
			{
				long valueOrDefault = dlcAppId.GetValueOrDefault();
				map[valueOrDefault] = depot;
			}
		}
		return map;
	}

	private string? DiffDisplayName(LuaEntry e)
	{
		long? num = _depotsById.GetValueOrDefault(e.Id)?.DlcAppId;
		if (num.HasValue)
		{
			long valueOrDefault = num.GetValueOrDefault();
			string text = _appList.GetName(valueOrDefault) ?? _appInfo.GetCached(valueOrDefault)?.Name;
			if (text != null)
			{
				return text;
			}
		}
		return _appList.GetName(e.Id) ?? _appInfo.GetCached(e.Id)?.Name ?? e.Comment;
	}

	private DiffRow ToDiffRow(LuaEntry e)
	{
		ContentDepot valueOrDefault = _depotsById.GetValueOrDefault(e.Id);
		bool flag = valueOrDefault?.IsDlc ?? false;
		bool flag2 = valueOrDefault?.IsShared ?? false;
		string? title = DiffDisplayName(e) ?? (flag ? string.Format(Strings.Manage_DlcName, valueOrDefault.DlcAppId) : (flag2 ? Strings.Manage_SharedDepot : (e.HasKey ? Strings.Manage_Depot : string.Format(Strings.Manage_DlcName, e.Id))));
		List<string> list = new List<string> { e.Id.ToString() };
		if ((object)valueOrDefault != null && valueOrDefault.Size > 0)
		{
			list.Add(FormatSize(valueOrDefault.Size));
		}
		if (!string.IsNullOrWhiteSpace(valueOrDefault?.Os))
		{
			list.Add(PrettyOs(valueOrDefault.Os));
		}
		if (!string.IsNullOrWhiteSpace(valueOrDefault?.Language))
		{
			list.Add(valueOrDefault.Language);
		}
		long? num = valueOrDefault?.DlcAppId;
		object obj;
		if (num.HasValue)
		{
			long valueOrDefault2 = num.GetValueOrDefault();
			obj = $"https://steamdb.info/app/{valueOrDefault2}/";
		}
		else
		{
			obj = $"https://steamdb.info/depot/{e.Id}/";
		}
		string steamDbUrl = (string)obj;
		return new DiffRow(title, string.Join("  ·  ", list), flag, flag2, steamDbUrl);
	}

	[RelayCommand]
	private static void OpenSteamDb(DiffRow row)
	{
		SteamService.OpenUrl(row.SteamDbUrl);
	}

	private static string FormatSize(long bytes)
	{
		if (bytes <= 0)
		{
			return "";
		}
		double num = (double)bytes / 1024.0 / 1024.0 / 1024.0;
		if (!(num >= 1.0))
		{
			return $"{(double)bytes / 1024.0 / 1024.0:0.#} MB";
		}
		return $"{num:0.##} GB";
	}

	private static string PrettyOs(string os)
	{
		switch (os)
		{
		case "windows":
			return "Windows";
		case "macos":
		case "macosx":
			return "macOS";
		case "linux":
			return "Linux";
		default:
			return os;
		}
	}

	private static LuaContents? ExtractLuaFromZip(string zipPath, long appId)
	{
		try
		{
			if (!IsZip(zipPath))
			{
				return LuaFileParser.Parse(zipPath, appId);
			}
			using ZipArchive zipArchive = ZipFile.OpenRead(zipPath);
			ZipArchiveEntry zipArchiveEntry = zipArchive.Entries.FirstOrDefault((ZipArchiveEntry e) => e.Name.EndsWith(".lua", StringComparison.OrdinalIgnoreCase));
			if (zipArchiveEntry == null)
			{
				return null;
			}
			string text = Path.Combine(Path.GetTempPath(), $"gabluchi_diff_{appId}_{Guid.NewGuid():N}.lua");
			zipArchiveEntry.ExtractToFile(text, overwrite: true);
			try
			{
				return LuaFileParser.Parse(text, appId);
			}
			finally
			{
				try
				{
					File.Delete(text);
				}
				catch
				{
				}
			}
		}
		catch
		{
			return null;
		}
	}

	[RelayCommand]
	private void RevealInstalled()
	{
		long? num = _installedAppId ?? Details?.AppId;
		if (num.HasValue)
		{
			long valueOrDefault = num.GetValueOrDefault();
			NavigateToGame?.Invoke(valueOrDefault);
		}
	}

	private void ResetResults()
	{
		Sources.Clear();
		SourcesLoaded = false;
		DlcInfo = null;
		DlcDepots.Clear();
		Error = null;
		LastDownload = null;
		_fastFetchSource = null;
	}

	private static string? ExtractAppId(string input)
	{
		string text = input.Trim();
		if (Regex.IsMatch(text, "^\\d{5,}$"))
		{
			return text;
		}
		Match match = Regex.Match(text, "/app/(\\d+)", RegexOptions.IgnoreCase);
		if (!match.Success)
		{
			return null;
		}
		return match.Groups[1].Value;
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnSearchTextChanged(string value)
	{
		if (!_suppressSearch)
		{
			ResetResults();
			Details = null;
			if (string.IsNullOrWhiteSpace(value))
			{
				InstallStatus = null;
			}
			string text = ExtractAppId(value);
			if (text != null)
			{
				IsResultsOpen = false;
				FetchDetailsDebouncedAsync(text);
			}
			else
			{
				SearchDebouncedAsync(value);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnFastFetchChanged(bool value)
	{
		_settings.FastFetch = value;
	}
}
