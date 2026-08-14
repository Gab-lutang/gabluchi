using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using GabLuchi.Resources;
using GabLuchi.Services;

namespace GabLuchi.ViewModels;

public class ManageViewModel : PagedListViewModel<LuaTileViewModel>
{
	private readonly SteamService _steam;

	private readonly SteamAppListCache _appList;

	private readonly SteamAppInfoCache _appInfo;

	private readonly CoverCache _covers;

	private readonly SteamDepotInfo _depotInfo;

	private readonly ToastService _toast;

	private readonly SettingsService _settings;

	private readonly SteamlessService _steamless;

	private List<LuaTileViewModel> _all = new List<LuaTileViewModel>();

	private List<LuaTileViewModel> _filtered = new List<LuaTileViewModel>();

	private CancellationTokenSource? _prefetchCts;

	[ObservableProperty]
	private string _searchText = "";

	[ObservableProperty]
	private bool _isFilterPanelOpen;

	public const string AnyOption = "Any";

	[ObservableProperty]
	[NotifyPropertyChangedFor("HasActiveFilters")]
	private string _selectedType = "Any";

	[ObservableProperty]
	[NotifyPropertyChangedFor("HasActiveFilters")]
	private string _selectedGenre = "Any";

	[ObservableProperty]
	[NotifyPropertyChangedFor("HasActiveFilters")]
	private string _selectedYear = "Any";

	[ObservableProperty]
	[NotifyPropertyChangedFor("HasActiveFilters")]
	private string _selectedPrice = "Any";

	[ObservableProperty]
	[NotifyPropertyChangedFor("HasActiveFilters")]
	private string _selectedContent = "Any";

	[ObservableProperty]
	private string _selectedSort = "Recently added";

	[ObservableProperty]
	[NotifyPropertyChangedFor("FilterPendingText")]
	private int _pendingDetailsCount;

	[ObservableProperty]
	private bool _hasPendingDetails;

	[ObservableProperty]
	[NotifyPropertyChangedFor("IsDetailOpen")]
	private LuaTileViewModel? _selectedTile;

	[ObservableProperty]
	private AppDetailViewModel? _detail;

	[ObservableProperty]
	[NotifyPropertyChangedFor("IsSelecting")]
	[NotifyPropertyChangedFor("SelectionLabel")]
	private int _selectedCount;

	[ObservableProperty]
	[NotifyPropertyChangedFor("NotBusy")]
	private bool _isBusy;

	[ObservableProperty]
	private double _progress;

	[ObservableProperty]
	private bool _isProgressIndeterminate;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand<LuaTileViewModel>? openDetailCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? toggleInLuaCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? toggleMissingCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? toggleUnknownCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand<DepotRow>? openSteamDbCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? closeDetailCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand<LuaTileViewModel>? openStorePageCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand<LuaTileViewModel>? openInSteamCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand<LuaTileViewModel>? revealFileCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand<LuaTileViewModel>? copyAppIdCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand<LuaTileViewModel>? updateCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand<LuaTileViewModel?>? removeDrmCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand<LuaTileViewModel>? deleteCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? clearSelectionCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? deleteSelectedCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? selectAllCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? copySelectedCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? removeDrmSelectedCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? refreshCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? clearFiltersCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? toggleFilterPanelCommand;

	public Action<long>? NavigateToAdd { get; set; }

	public ObservableCollection<string> TypeOptions { get; } = new ObservableCollection<string> { "Any" };

	public ObservableCollection<string> GenreOptions { get; } = new ObservableCollection<string> { "Any" };

	public ObservableCollection<string> YearOptions { get; } = new ObservableCollection<string> { "Any" };

	public ObservableCollection<string> PriceOptions { get; } = new ObservableCollection<string> { "Any", "Free", "Paid" };

	public ObservableCollection<string> ContentOptions { get; } = new ObservableCollection<string> { "Any", "Hide adult", "Adult only" };

	public ObservableCollection<string> SortOptions { get; } = new ObservableCollection<string> { "Recently added", "Name (A–Z)", "Release date (newest)", "Metacritic", "Most reviewed" };

	public bool HasActiveFilters
	{
		get
		{
			if (!(SelectedType != "Any") && !(SelectedGenre != "Any") && !(SelectedYear != "Any") && !(SelectedPrice != "Any"))
			{
				return SelectedContent != "Any";
			}
			return true;
		}
	}

	public string FilterPendingText => string.Format(Strings.Manage_FetchingDetails, PendingDetailsCount);

	public bool IsDetailOpen => SelectedTile != null;

	public bool IsSelecting => SelectedCount > 0;

	public string SelectionLabel => string.Format(Strings.Manage_SelectionLabel, SelectedCount);

	public bool NotBusy => !IsBusy;

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
				_searchText = value;
				OnSearchTextChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SearchText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsFilterPanelOpen
	{
		get
		{
			return _isFilterPanelOpen;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isFilterPanelOpen, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsFilterPanelOpen);
				_isFilterPanelOpen = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsFilterPanelOpen);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string SelectedType
	{
		get
		{
			return _selectedType;
		}
		[MemberNotNull("_selectedType")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_selectedType, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedType);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HasActiveFilters);
				_selectedType = value;
				OnSelectedTypeChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedType);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HasActiveFilters);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string SelectedGenre
	{
		get
		{
			return _selectedGenre;
		}
		[MemberNotNull("_selectedGenre")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_selectedGenre, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedGenre);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HasActiveFilters);
				_selectedGenre = value;
				OnSelectedGenreChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedGenre);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HasActiveFilters);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string SelectedYear
	{
		get
		{
			return _selectedYear;
		}
		[MemberNotNull("_selectedYear")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_selectedYear, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedYear);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HasActiveFilters);
				_selectedYear = value;
				OnSelectedYearChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedYear);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HasActiveFilters);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string SelectedPrice
	{
		get
		{
			return _selectedPrice;
		}
		[MemberNotNull("_selectedPrice")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_selectedPrice, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedPrice);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HasActiveFilters);
				_selectedPrice = value;
				OnSelectedPriceChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedPrice);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HasActiveFilters);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string SelectedContent
	{
		get
		{
			return _selectedContent;
		}
		[MemberNotNull("_selectedContent")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_selectedContent, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedContent);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HasActiveFilters);
				_selectedContent = value;
				OnSelectedContentChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedContent);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HasActiveFilters);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string SelectedSort
	{
		get
		{
			return _selectedSort;
		}
		[MemberNotNull("_selectedSort")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_selectedSort, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedSort);
				_selectedSort = value;
				OnSelectedSortChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedSort);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int PendingDetailsCount
	{
		get
		{
			return _pendingDetailsCount;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_pendingDetailsCount, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PendingDetailsCount);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.FilterPendingText);
				_pendingDetailsCount = value;
				OnPendingDetailsCountChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PendingDetailsCount);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.FilterPendingText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool HasPendingDetails
	{
		get
		{
			return _hasPendingDetails;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_hasPendingDetails, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HasPendingDetails);
				_hasPendingDetails = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HasPendingDetails);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public LuaTileViewModel? SelectedTile
	{
		get
		{
			return _selectedTile;
		}
		set
		{
			if (!EqualityComparer<LuaTileViewModel>.Default.Equals(_selectedTile, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedTile);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsDetailOpen);
				_selectedTile = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedTile);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsDetailOpen);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public AppDetailViewModel? Detail
	{
		get
		{
			return _detail;
		}
		set
		{
			if (!EqualityComparer<AppDetailViewModel>.Default.Equals(_detail, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Detail);
				_detail = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Detail);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int SelectedCount
	{
		get
		{
			return _selectedCount;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_selectedCount, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedCount);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsSelecting);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectionLabel);
				_selectedCount = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedCount);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsSelecting);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectionLabel);
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

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<LuaTileViewModel> OpenDetailCommand => openDetailCommand ?? (openDetailCommand = new AsyncRelayCommand<LuaTileViewModel>(OpenDetailAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ToggleInLuaCommand => toggleInLuaCommand ?? (toggleInLuaCommand = new RelayCommand(ToggleInLua));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ToggleMissingCommand => toggleMissingCommand ?? (toggleMissingCommand = new RelayCommand(ToggleMissing));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ToggleUnknownCommand => toggleUnknownCommand ?? (toggleUnknownCommand = new RelayCommand(ToggleUnknown));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<DepotRow> OpenSteamDbCommand => openSteamDbCommand ?? (openSteamDbCommand = new RelayCommand<DepotRow>(OpenSteamDb));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand CloseDetailCommand => closeDetailCommand ?? (closeDetailCommand = new RelayCommand(CloseDetail));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<LuaTileViewModel> OpenStorePageCommand => openStorePageCommand ?? (openStorePageCommand = new RelayCommand<LuaTileViewModel>(OpenStorePage));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<LuaTileViewModel> OpenInSteamCommand => openInSteamCommand ?? (openInSteamCommand = new RelayCommand<LuaTileViewModel>(OpenInSteam));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<LuaTileViewModel> RevealFileCommand => revealFileCommand ?? (revealFileCommand = new RelayCommand<LuaTileViewModel>(RevealFile));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<LuaTileViewModel> CopyAppIdCommand => copyAppIdCommand ?? (copyAppIdCommand = new RelayCommand<LuaTileViewModel>(CopyAppId));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<LuaTileViewModel> UpdateCommand => updateCommand ?? (updateCommand = new RelayCommand<LuaTileViewModel>(Update));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<LuaTileViewModel?> RemoveDrmCommand => removeDrmCommand ?? (removeDrmCommand = new AsyncRelayCommand<LuaTileViewModel>(RemoveDrm));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<LuaTileViewModel> DeleteCommand => deleteCommand ?? (deleteCommand = new RelayCommand<LuaTileViewModel>(Delete));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ClearSelectionCommand => clearSelectionCommand ?? (clearSelectionCommand = new RelayCommand(ClearSelection));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand DeleteSelectedCommand => deleteSelectedCommand ?? (deleteSelectedCommand = new RelayCommand(DeleteSelected));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand SelectAllCommand => selectAllCommand ?? (selectAllCommand = new RelayCommand(SelectAll));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand CopySelectedCommand => copySelectedCommand ?? (copySelectedCommand = new RelayCommand(CopySelected));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand RemoveDrmSelectedCommand => removeDrmSelectedCommand ?? (removeDrmSelectedCommand = new AsyncRelayCommand(RemoveDrmSelected));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand RefreshCommand => refreshCommand ?? (refreshCommand = new AsyncRelayCommand(Refresh));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ClearFiltersCommand => clearFiltersCommand ?? (clearFiltersCommand = new RelayCommand(ClearFilters));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand ToggleFilterPanelCommand => toggleFilterPanelCommand ?? (toggleFilterPanelCommand = new RelayCommand(ToggleFilterPanel));

	protected override void SavePageSizeSetting(int size)
	{
		_settings.ManagePageSize = size;
	}

	public ManageViewModel(SteamService steam, SteamAppListCache appList, SteamAppInfoCache appInfo, CoverCache covers, SteamDepotInfo depotInfo, ToastService toast, SettingsService settings, SteamlessService steamless)
	{
		_steam = steam;
		_appList = appList;
		_appInfo = appInfo;
		_covers = covers;
		_depotInfo = depotInfo;
		_toast = toast;
		_settings = settings;
		_steamless = steamless;
		InitPageSize(settings.ManagePageSize);
	}

	public void ResolveTile(LuaTileViewModel tile)
	{
		tile.EnsureResolvedAsync(_appInfo, _covers);
	}

	public async Task OpenDetailForAppIdAsync(long appId)
	{
		LuaTileViewModel luaTileViewModel = _all.FirstOrDefault((LuaTileViewModel t) => t.AppId == appId);
		if (luaTileViewModel == null)
		{
			await LoadAsync();
			luaTileViewModel = _all.FirstOrDefault((LuaTileViewModel t) => t.AppId == appId);
		}
		if (luaTileViewModel != null)
		{
			await OpenDetailAsync(luaTileViewModel);
		}
		else
		{
			_toast.Show(Strings.Manage_Toast_NotFound_Title, Strings.Manage_Toast_NotFound_Body, error: true);
		}
	}

	[RelayCommand]
	private async Task OpenDetailAsync(LuaTileViewModel tile)
	{
		AppDetailViewModel detail = (Detail = new AppDetailViewModel(tile.AppId));
		SelectedTile = tile;
		tile.EnsureResolvedAsync(_appInfo, _covers);
		LuaContents luaContents = await Task.Run(() => LuaFileParser.Parse(tile.FilePath, tile.AppId));
		HashSet<long> luaIds = (((object)luaContents == null) ? new HashSet<long>() : luaContents.Entries.Select((LuaEntry e) => e.Id).ToHashSet());
		Dictionary<long, string> luaNames = (((object)luaContents == null) ? new Dictionary<long, string>() : luaContents.Entries.Where((LuaEntry e) => e.Comment != null).ToDictionary((LuaEntry e) => e.Id, (LuaEntry e) => e.Comment));
		if (Detail != detail)
		{
			return;
		}
		AppDepotInfo info = await _depotInfo.GetAsync(tile.AppId);
		if (Detail != detail)
		{
			return;
		}
		if ((object)info == null)
		{
			detail.Error = Strings.Manage_DepotError;
			detail.IsLoading = false;
			return;
		}
		BuildRows(detail, info, luaIds, luaNames);
		detail.IsLoading = false;
		List<long> list = (from a in (from d in info.Depots
				where d.DlcAppId.HasValue
				select d.DlcAppId.Value).Concat(info.DlcIds).Distinct()
			where _appList.GetName(a) == null && _appInfo.GetCached(a)?.Name == null && !luaNames.ContainsKey(a)
			select a).ToList();
		if (list.Count > 0)
		{
			await Parallel.ForEachAsync(list, new ParallelOptions
			{
				MaxDegreeOfParallelism = 4
			}, async delegate(long id, CancellationToken _)
			{
				await _appInfo.ResolveAsync(id);
			});
			if (Detail == detail)
			{
				BuildRows(detail, info, luaIds, luaNames);
			}
		}
	}

	private void BuildRows(AppDetailViewModel detail, AppDepotInfo info, HashSet<long> luaIds, IReadOnlyDictionary<long, string> luaNames)
	{
		List<ContentDepot> list = new List<ContentDepot>(info.Depots);
		HashSet<long> hashSet = (from d in info.Depots
			where d.DlcAppId.HasValue
			select d.DlcAppId.Value).ToHashSet();
		foreach (long dlcId in info.DlcIds)
		{
			if (!hashSet.Contains(dlcId))
			{
				list.Add(new ContentDepot(dlcId, 0L, dlcId, IsShared: false, null, null));
			}
		}
		detail.InLua = list.Where(IsInLua).Select(Row).ToList();
		detail.Missing = list.Where((ContentDepot d) => !IsInLua(d) && !IsUnknown(d)).Select(Row).ToList();
		detail.Unknown = list.Where((ContentDepot d) => !IsInLua(d) && IsUnknown(d)).Select(Row).ToList();
		bool DlcNameKnown(long dlcId)
		{
			if (_appList.GetName(dlcId) == null && _appInfo.GetCached(dlcId)?.Name == null)
			{
				return luaNames.ContainsKey(dlcId);
			}
			return true;
		}
		bool IsInLua(ContentDepot d)
		{
			if (!luaIds.Contains(d.Id))
			{
				long? dlcAppId = d.DlcAppId;
				if (dlcAppId.HasValue)
				{
					long valueOrDefault = dlcAppId.GetValueOrDefault();
					return luaIds.Contains(valueOrDefault);
				}
				return false;
			}
			return true;
		}
		bool IsUnknown(ContentDepot d)
		{
			if (!d.IsShared && (!d.IsDlc || DlcNameKnown(d.DlcAppId.Value)))
			{
				if (!d.IsDlc)
				{
					return d.Size == 0;
				}
				return false;
			}
			return true;
		}
		DepotRow Row(ContentDepot d)
		{
			long? dlcAppId = d.DlcAppId;
			object obj;
			if (dlcAppId.HasValue)
			{
				long valueOrDefault = dlcAppId.GetValueOrDefault();
				obj = _appList.GetName(valueOrDefault) ?? _appInfo.GetCached(valueOrDefault)?.Name;
			}
			else
			{
				obj = null;
			}
			if (obj == null)
			{
				obj = luaNames.GetValueOrDefault(d.Id) ?? (d.IsDlc ? string.Format(Strings.Manage_DlcName, d.DlcAppId) : (d.IsShared ? Strings.Manage_SharedDepot : Strings.Manage_Depot));
			}
			string title = (string)obj;
			List<string> list2 = new List<string> { d.Id.ToString() };
			if (d.Size > 0)
			{
				list2.Add(FormatSize(d.Size));
			}
			if (!string.IsNullOrWhiteSpace(d.Os))
			{
				list2.Add(PrettyOs(d.Os));
			}
			if (!string.IsNullOrWhiteSpace(d.Language))
			{
				list2.Add(d.Language);
			}
			dlcAppId = d.DlcAppId;
			object obj2;
			if (dlcAppId.HasValue)
			{
				long valueOrDefault2 = dlcAppId.GetValueOrDefault();
				obj2 = $"https://steamdb.info/app/{valueOrDefault2}/";
			}
			else
			{
				obj2 = $"https://steamdb.info/depot/{d.Id}/";
			}
			string steamDbUrl = (string)obj2;
			return new DepotRow(d.Id, title, string.Join("  ·  ", list2), d.IsDlc, d.IsShared, steamDbUrl);
		}
	}

	[RelayCommand]
	private void ToggleInLua()
	{
		AppDetailViewModel detail = Detail;
		if (detail != null)
		{
			detail.IsInLuaExpanded = !detail.IsInLuaExpanded;
		}
	}

	[RelayCommand]
	private void ToggleMissing()
	{
		AppDetailViewModel detail = Detail;
		if (detail != null)
		{
			detail.IsMissingExpanded = !detail.IsMissingExpanded;
		}
	}

	[RelayCommand]
	private void ToggleUnknown()
	{
		AppDetailViewModel detail = Detail;
		if (detail != null)
		{
			detail.IsUnknownExpanded = !detail.IsUnknownExpanded;
		}
	}

	[RelayCommand]
	private static void OpenSteamDb(DepotRow row)
	{
		SteamService.OpenUrl(row.SteamDbUrl);
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

	[RelayCommand]
	private void CloseDetail()
	{
		SelectedTile = null;
		Detail = null;
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
			double value = (double)bytes / 1024.0 / 1024.0;
			return $"{value:0.#} MB";
		}
		return $"{num:0.##} GB";
	}

	[RelayCommand]
	private static void OpenStorePage(LuaTileViewModel tile)
	{
		SteamService.OpenUrl($"steam://store/{tile.AppId}");
	}

	[RelayCommand]
	private static void OpenInSteam(LuaTileViewModel tile)
	{
		SteamService.OpenUrl($"steam://nav/games/details/{tile.AppId}");
	}

	[RelayCommand]
	private static void RevealFile(LuaTileViewModel tile)
	{
		SteamService.RevealInExplorer(tile.FilePath);
	}

	[RelayCommand]
	private static void CopyAppId(LuaTileViewModel tile)
	{
		Clipboard.SetText(tile.AppId.ToString());
	}

	[RelayCommand]
	private void Update(LuaTileViewModel tile)
	{
		NavigateToAdd?.Invoke(tile.AppId);
	}

	[RelayCommand]
	private async Task RemoveDrm(LuaTileViewModel? tile)
	{
		if (tile == null || IsBusy || MessageBox.Show(Strings.Manage_Steamless_Confirm_Body, Strings.Manage_Steamless_Confirm_Title, MessageBoxButton.OKCancel, MessageBoxImage.Exclamation) != MessageBoxResult.OK)
		{
			return;
		}
		IsBusy = true;
		IsProgressIndeterminate = true;
		Progress = 0.0;
		Progress<double?> progress = new Progress<double?>(delegate(double? p)
		{
			IsProgressIndeterminate = !p.HasValue;
			if (p.HasValue)
			{
				Progress = p.Value * 100.0;
			}
		});
		try
		{
			SteamlessResult steamlessResult = await _steamless.PatchGameAsync(tile.AppId, progress);
			if (steamlessResult.Failed)
			{
				string error = steamlessResult.Error;
				string text = ((error == "no-install") ? Strings.Manage_Steamless_NoInstall : ((!(error == "no-exe")) ? string.Format(Strings.Manage_Steamless_Failed, "") : Strings.Manage_Steamless_NoInstall));
				string message = text;
				_toast.Show(Strings.Manage_Action_RemoveDrm, message, error: true);
			}
			else
			{
				_toast.Show(Strings.Manage_Action_RemoveDrm, string.Format(Strings.Manage_Toast_Steamless_Done, steamlessResult.Patched, steamlessResult.Unchanged));
			}
		}
		catch (Exception ex)
		{
			_toast.Show(Strings.Manage_Action_RemoveDrm, string.Format(Strings.Manage_Steamless_Failed, ex.Message), error: true);
		}
		finally
		{
			IsBusy = false;
			IsProgressIndeterminate = false;
		}
	}

	[RelayCommand]
	private void Delete(LuaTileViewModel tile)
	{
		if (MessageBox.Show(string.Format(Strings.Manage_Delete_Body, tile.Name, tile.AppId), Strings.Manage_Delete_Title, MessageBoxButton.OKCancel, MessageBoxImage.Exclamation) != MessageBoxResult.OK)
		{
			return;
		}
		try
		{
			if (File.Exists(tile.FilePath))
			{
				File.Delete(tile.FilePath);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(string.Format(Strings.Manage_RemoveFailed_File, ex.Message), Strings.Manage_RemoveFailed_Title, MessageBoxButton.OK, MessageBoxImage.Hand);
			return;
		}
		if (TryDeleteFile(tile.FilePath, tile.Name))
		{
			RemoveTile(tile);
		}
	}

	private void RecountSelection()
	{
		SelectedCount = _all.Count((LuaTileViewModel t) => t.IsSelected);
	}

	[RelayCommand]
	private void ClearSelection()
	{
		foreach (LuaTileViewModel item in _all.Where((LuaTileViewModel t) => t.IsSelected))
		{
			item.IsSelected = false;
		}
		SelectedCount = 0;
	}

	[RelayCommand]
	private void DeleteSelected()
	{
		List<LuaTileViewModel> list = _all.Where((LuaTileViewModel t) => t.IsSelected).ToList();
		if (list.Count == 0 || MessageBox.Show(string.Format(Strings.Manage_DeleteMany_Body, list.Count), Strings.Manage_DeleteMany_Title, MessageBoxButton.OKCancel, MessageBoxImage.Exclamation) != MessageBoxResult.OK)
		{
			return;
		}
		int num = 0;
		foreach (LuaTileViewModel item in list)
		{
			if (TryDeleteFile(item.FilePath, item.Name, silent: true))
			{
				item.SelectionChanged = null;
				_all.Remove(item);
				if (SelectedTile == item)
				{
					CloseDetail();
				}
			}
			else
			{
				num++;
			}
		}
		ApplyFilter();
		SelectedCount = _all.Count((LuaTileViewModel t) => t.IsSelected);
		if (num > 0)
		{
			MessageBox.Show(string.Format(Strings.Manage_RemoveFailed_Count, num), Strings.Manage_RemoveFailed_Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
		}
		OfferRestartSteam();
	}

	[RelayCommand]
	private void SelectAll()
	{
		List<LuaTileViewModel> current = _filtered;
		if (current.Count == 0)
		{
			return;
		}
		bool allSelected = current.All((LuaTileViewModel t) => t.IsSelected);
		foreach (LuaTileViewModel item in current)
		{
			item.IsSelected = !allSelected;
		}
		RecountSelection();
	}

	[RelayCommand]
	private void CopySelected()
	{
		List<string> ids = _all.Where((LuaTileViewModel t) => t.IsSelected).Select((LuaTileViewModel t) => t.AppId.ToString()).ToList();
		if (ids.Count == 0)
		{
			return;
		}
		Clipboard.SetText(string.Join("\n", ids));
		_toast.Show(Strings.Manage_Toast_Copied_Title, string.Format(Strings.Manage_Toast_Copied_Body, ids.Count));
	}

	[RelayCommand]
	private async Task RemoveDrmSelected()
	{
		List<LuaTileViewModel> list = _all.Where((LuaTileViewModel t) => t.IsSelected).ToList();
		if (list.Count == 0 || IsBusy || MessageBox.Show(string.Format(Strings.Manage_Steamless_Many_Body, list.Count), Strings.Manage_Steamless_Many_Title, MessageBoxButton.OKCancel, MessageBoxImage.Exclamation) != MessageBoxResult.OK)
		{
			return;
		}
		IsBusy = true;
		IsProgressIndeterminate = true;
		Progress = 0.0;
		int done = 0;
		int patched = 0;
		int unchanged = 0;
		int failed = 0;
		try
		{
			foreach (LuaTileViewModel tile in list)
			{
				SteamlessResult result = await _steamless.PatchGameAsync(tile.AppId, new Progress<double?>(delegate(double? p)
				{
					IsProgressIndeterminate = !p.HasValue;
					if (p.HasValue)
					{
						Progress = ((double)done + p.Value) / (double)list.Count * 100.0;
					}
				}));
				if (result.Failed)
				{
					if (result.Error == "no-install" || result.Error == "no-exe")
					{
						unchanged++;
					}
					else
					{
						failed++;
					}
				}
				else
				{
					patched += result.Patched;
					unchanged += result.Unchanged;
				}
				done++;
				OnUi(delegate
				{
					Progress = (double)done / (double)list.Count * 100.0;
				});
			}
			_toast.Show(Strings.Manage_Action_RemoveDrm, string.Format(Strings.Manage_Toast_Steamless_Many, patched, unchanged, failed));
		}
		catch (Exception ex)
		{
			_toast.Show(Strings.Manage_Action_RemoveDrm, string.Format(Strings.Manage_Steamless_Failed, ex.Message), error: true);
		}
		finally
		{
			IsBusy = false;
			IsProgressIndeterminate = false;
		}
	}

	private void OfferRestartSteam()
	{
		if (MessageBox.Show(Strings.Manage_RestartSteam_Ask, Strings.Manage_RestartSteam_Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes && !_steam.RestartSteam())
		{
			MessageBox.Show(Strings.Manage_RestartSteam_Failed, Strings.Manage_RestartSteam_Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
		}
	}

	private static bool TryDeleteFile(string path, string name, bool silent = false)
	{
		try
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
			return true;
		}
		catch (Exception ex)
		{
			if (!silent)
			{
				MessageBox.Show(string.Format(Strings.Manage_RemoveFailed_Named, name, ex.Message), Strings.Manage_RemoveFailed_Title, MessageBoxButton.OK, MessageBoxImage.Hand);
			}
			return false;
		}
	}

	private void RemoveTile(LuaTileViewModel tile)
	{
		tile.SelectionChanged = null;
		_all.Remove(tile);
		if (SelectedTile == tile)
		{
			CloseDetail();
		}
		ApplyFilter();
	}

	public async Task LoadAsync()
	{
		if (base.IsLoading)
		{
			return;
		}
		base.IsLoading = true;
		try
		{
			string effectivePath = _steam.EffectivePath;
			if (effectivePath == null)
			{
				_all = new List<LuaTileViewModel>();
				ApplyFilter();
				SetEmpty(Strings.Manage_Empty_NoSteam);
				return;
			}
			string dir = Path.Combine(effectivePath, "config", "stplug-in");
			if (!Directory.Exists(dir))
			{
				_all = new List<LuaTileViewModel>();
				ApplyFilter();
				SetEmpty(Strings.Manage_Empty_NoLuas);
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
					string text = _appList.GetName(num) ?? ParseLuaName(f.path) ?? _appInfo.GetCached(num)?.Name;
					bool nameIsPlaceholder = text == null;
					DateTime addedAt = ((fileInfo.LastWriteTime > fileInfo.CreationTime) ? fileInfo.LastWriteTime : fileInfo.CreationTime);
					return new LuaTileViewModel(num, f.path, addedAt, text ?? string.Format(Strings.Common_AppFallback, num), nameIsPlaceholder);
				})
				orderby t.AddedAt descending
				select t).ToList());
			foreach (LuaTileViewModel item in list)
			{
				item.SelectionChanged = RecountSelection;
			}
			_all = list;
			SelectedCount = 0;
			PopulateFilterOptions();
			ApplyFilter();
			if (_all.Count == 0)
			{
				SetEmpty(Strings.Manage_Empty_NoLuas);
			}
			StartCoverPrefetch(_all);
		}
		finally
		{
			base.IsLoading = false;
		}
	}

	private void StartCoverPrefetch(IReadOnlyList<LuaTileViewModel> tiles)
	{
		_prefetchCts?.Cancel();
		CancellationTokenSource cts = (_prefetchCts = new CancellationTokenSource());
		List<long> appids = tiles.Select((LuaTileViewModel t) => t.AppId).ToList();
		Task.Run(async delegate
		{
			_ = 1;
			try
			{
				await Parallel.ForEachAsync(appids, new ParallelOptions
				{
					MaxDegreeOfParallelism = 8,
					CancellationToken = cts.Token
				}, async delegate(long appid, CancellationToken _)
				{
					try
					{
						await LuaTileViewModel.ResolveCoverFileAsync(appid, _appInfo, _covers);
					}
					catch
					{
					}
				});
				DateTime lastUi = DateTime.MinValue;
				await _appInfo.BackfillFullDetailsAsync(appids, delegate
				{
					if (!cts.Token.IsCancellationRequested && !(DateTime.UtcNow - lastUi < TimeSpan.FromSeconds(2.0)))
					{
						lastUi = DateTime.UtcNow;
						OnUi(delegate
						{
							PopulateFilterOptions();
							RefreshPendingCount();
						});
					}
				}, cts.Token);
				if (!cts.Token.IsCancellationRequested)
				{
					OnUi(delegate
					{
						PopulateFilterOptions();
						ApplyFilter(resetPage: false);
					});
				}
			}
			catch (OperationCanceledException)
			{
			}
		});
	}

	private static void OnUi(Action action)
	{
		Application current = Application.Current;
		Dispatcher val = ((current != null) ? ((DispatcherObject)current).Dispatcher : null);
		if (val == null || val.CheckAccess())
		{
			action();
		}
		else
		{
			val.Invoke(action);
		}
	}

	[RelayCommand]
	private Task Refresh()
	{
		return RefreshWithCooldownAsync(async delegate
		{
			if (!string.IsNullOrEmpty(SearchText))
			{
				SearchText = "";
			}
			await LoadAsync();
			_toast.Show(Strings.Manage_Toast_Refreshed_Title, string.Format(Strings.Manage_Toast_Refreshed_Body, _all.Count));
		});
	}

	private void SetEmpty(string message)
	{
		base.EmptyMessage = message;
	}

	private void RefreshPendingCount()
	{
		PendingDetailsCount = _all.Count((LuaTileViewModel t) => (object)_appInfo.GetFilterData(t.AppId) == null);
	}

	private void ApplyFilter(bool resetPage = true)
	{
		string q = SearchText.Trim();
		bool filtersActive = HasActiveFilters;
		IEnumerable<LuaTileViewModel> source = _all;
		if (!string.IsNullOrEmpty(q))
		{
			source = source.Where((LuaTileViewModel t) => t.Matches(q));
		}
		source = source.Where(delegate(LuaTileViewModel t)
		{
			AppFilterData filterData = _appInfo.GetFilterData(t.AppId);
			t.DetailsLoaded = (object)filterData != null;
			t.UpdateReleaseLabel(_appInfo);
			if (!filtersActive)
			{
				return true;
			}
			return (object)filterData != null && MatchesFilters(filterData);
		});
		List<LuaTileViewModel> tiles = source.ToList();
		tiles = SortTiles(tiles);
		_filtered = tiles;
		PendingDetailsCount = _all.Count((LuaTileViewModel t) => (object)_appInfo.GetFilterData(t.AppId) == null);
		base.EmptyMessage = ((_all.Count > 0) ? Strings.Manage_Empty_NoMatch : Strings.Manage_Empty_NoLuas);
		SetFiltered(tiles, resetPage);
	}

	private bool MatchesFilters(AppFilterData d)
	{
		if (SelectedType != "Any" && !string.Equals(d.Type, SelectedType, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (SelectedGenre != "Any" && !d.Genres.Any((string g) => string.Equals(g, SelectedGenre, StringComparison.OrdinalIgnoreCase)))
		{
			return false;
		}
		if (SelectedYear != "Any" && (!d.ReleaseYear.HasValue || d.ReleaseYear.Value.ToString() != SelectedYear))
		{
			return false;
		}
		if (SelectedPrice != "Any")
		{
			if (SelectedPrice == "Free" && !d.IsFree)
			{
				return false;
			}
			if (SelectedPrice == "Paid" && d.IsFree)
			{
				return false;
			}
		}
		if (SelectedContent == "Hide adult" && d.IsAdult)
		{
			return false;
		}
		if (SelectedContent == "Adult only" && !d.IsAdult)
		{
			return false;
		}
		return true;
	}

	private List<LuaTileViewModel> SortTiles(List<LuaTileViewModel> tiles)
	{
		return SelectedSort switch
		{
			"Name (A–Z)" => tiles.OrderBy<LuaTileViewModel, string>((LuaTileViewModel t) => t.Name, StringComparer.OrdinalIgnoreCase).ToList(), 
			"Release date (newest)" => tiles.OrderByDescending((LuaTileViewModel t) => _appInfo.GetFilterData(t.AppId)?.ReleaseDate ?? DateTime.MinValue).ToList(), 
			"Metacritic" => tiles.OrderByDescending((LuaTileViewModel t) => _appInfo.GetFilterData(t.AppId)?.Metacritic ?? int.MinValue).ToList(), 
			"Most reviewed" => tiles.OrderByDescending((LuaTileViewModel t) => _appInfo.GetFilterData(t.AppId)?.Reviews ?? long.MinValue).ToList(), 
			_ => tiles.OrderByDescending((LuaTileViewModel t) => t.AddedAt).ToList(), 
		};
	}

	[RelayCommand]
	private void ClearFilters()
	{
		SelectedType = "Any";
		SelectedGenre = "Any";
		SelectedYear = "Any";
		SelectedPrice = "Any";
		SelectedContent = "Any";
		OnPropertyChanged("HasActiveFilters");
		ApplyFilter();
	}

	[RelayCommand]
	private void ToggleFilterPanel()
	{
		IsFilterPanelOpen = !IsFilterPanelOpen;
	}

	private void PopulateFilterOptions()
	{
		SortedSet<string> sortedSet = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
		SortedSet<string> sortedSet2 = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
		SortedSet<int> sortedSet3 = new SortedSet<int>();
		foreach (LuaTileViewModel item in _all)
		{
			AppFilterData filterData = _appInfo.GetFilterData(item.AppId);
			if ((object)filterData == null)
			{
				continue;
			}
			if (!string.IsNullOrWhiteSpace(filterData.Type))
			{
				sortedSet.Add(filterData.Type);
			}
			foreach (string genre in filterData.Genres)
			{
				sortedSet2.Add(genre);
			}
			int? releaseYear = filterData.ReleaseYear;
			if (releaseYear.HasValue)
			{
				int valueOrDefault = releaseYear.GetValueOrDefault();
				sortedSet3.Add(valueOrDefault);
			}
		}
		RebuildOptionList(TypeOptions, sortedSet);
		RebuildOptionList(GenreOptions, sortedSet2);
		RebuildOptionList(YearOptions, from y in sortedSet3
			orderby y descending
			select y.ToString());
	}

	private static void RebuildOptionList(ObservableCollection<string> target, IEnumerable<string> values)
	{
		List<string> list = values.ToList();
		if (target.Count == 0 || target[0] != "Any")
		{
			target.Insert(0, "Any");
		}
		for (int num = target.Count - 1; num >= 1; num--)
		{
			if (!list.Contains(target[num]))
			{
				target.RemoveAt(num);
			}
		}
		foreach (string item in list)
		{
			if (!target.Contains(item))
			{
				target.Add(item);
			}
		}
	}

	private static string? ParseLuaName(string path)
	{
		try
		{
			using StreamReader streamReader = new StreamReader(path);
			string text = streamReader.ReadLine();
			string text2 = streamReader.ReadLine();
			if (text != null && text2 != null && text.Contains("Created by", StringComparison.OrdinalIgnoreCase) && text2.StartsWith("--"))
			{
				string text3 = text2.TrimStart('-', ' ').Trim();
				return string.IsNullOrWhiteSpace(text3) ? null : text3;
			}
		}
		catch
		{
		}
		return null;
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnSearchTextChanged(string value)
	{
		ApplyFilter();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnSelectedTypeChanged(string value)
	{
		ApplyFilter();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnSelectedGenreChanged(string value)
	{
		ApplyFilter();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnSelectedYearChanged(string value)
	{
		ApplyFilter();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnSelectedPriceChanged(string value)
	{
		ApplyFilter();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnSelectedContentChanged(string value)
	{
		ApplyFilter();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnSelectedSortChanged(string value)
	{
		ApplyFilter();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnPendingDetailsCountChanged(int value)
	{
		HasPendingDetails = value > 0;
	}
}
