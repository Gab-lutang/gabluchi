using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using GabLuchi.Models;
using GabLuchi.Resources;
using GabLuchi.Services;

namespace GabLuchi.ViewModels;

public class FixesViewModel : PagedListViewModel<FixGameCardVm>
{
	private readonly GabLuchiApiClient api;

	private readonly ManifestDownloader manifestDownloader;

	private readonly LuaInstaller installer;

	private readonly SteamService steam;

	private readonly SteamLibraryService library;

	private readonly CoverCache covers;

	private readonly ToastService toast;

	private readonly SettingsService settings;

	private List<FixGameCardVm> _allGames = new List<FixGameCardVm>();

	[ObservableProperty]
	private string _searchText = "";

	[ObservableProperty]
	private string? _selectedTagId;

	[ObservableProperty]
	[NotifyPropertyChangedFor("IsDetailOpen")]
	private FixGameCardVm? _selectedGame;

	[ObservableProperty]
	private bool _isLoadingFixes;

	private List<FixItemVm> _allFixes = new List<FixItemVm>();

	[ObservableProperty]
	[NotifyPropertyChangedFor("HasFixTags")]
	private string? _selectedFixTagId;

	[ObservableProperty]
	[NotifyPropertyChangedFor("NotBusy")]
	private bool _isBusy;

	[ObservableProperty]
	private double _progress;

	[ObservableProperty]
	private bool _isProgressIndeterminate;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? refreshCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand<string?>? selectTagCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand<FixGameCardVm>? openGameCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand<string?>? selectFixTagCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? closeDetailCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand<FixItemVm>? downloadManifestCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand<FixItemVm>? downloadFixCommand;

	public Func<Task>? RequestSignIn { get; set; }

	public ObservableCollection<TagPillVm> Tags { get; } = new ObservableCollection<TagPillVm>();

	public bool IsDetailOpen => SelectedGame != null;

	public ObservableCollection<FixItemVm> Fixes { get; } = new ObservableCollection<FixItemVm>();

	public ObservableCollection<TagPillVm> FixTags { get; } = new ObservableCollection<TagPillVm>();

	public bool HasFixTags => FixTags.Count > 0;

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
	public string? SelectedTagId
	{
		get
		{
			return _selectedTagId;
		}
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_selectedTagId, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedTagId);
				_selectedTagId = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedTagId);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public FixGameCardVm? SelectedGame
	{
		get
		{
			return _selectedGame;
		}
		set
		{
			if (!EqualityComparer<FixGameCardVm>.Default.Equals(_selectedGame, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedGame);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsDetailOpen);
				_selectedGame = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedGame);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsDetailOpen);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsLoadingFixes
	{
		get
		{
			return _isLoadingFixes;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isLoadingFixes, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsLoadingFixes);
				_isLoadingFixes = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsLoadingFixes);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string? SelectedFixTagId
	{
		get
		{
			return _selectedFixTagId;
		}
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_selectedFixTagId, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedFixTagId);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HasFixTags);
				_selectedFixTagId = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedFixTagId);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HasFixTags);
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
	public IAsyncRelayCommand RefreshCommand => refreshCommand ?? (refreshCommand = new AsyncRelayCommand(Refresh));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<string?> SelectTagCommand => selectTagCommand ?? (selectTagCommand = new RelayCommand<string>(SelectTag));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<FixGameCardVm> OpenGameCommand => openGameCommand ?? (openGameCommand = new AsyncRelayCommand<FixGameCardVm>(OpenGame));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<string?> SelectFixTagCommand => selectFixTagCommand ?? (selectFixTagCommand = new RelayCommand<string>(SelectFixTag));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand CloseDetailCommand => closeDetailCommand ?? (closeDetailCommand = new RelayCommand(CloseDetail));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<FixItemVm> DownloadManifestCommand => downloadManifestCommand ?? (downloadManifestCommand = new AsyncRelayCommand<FixItemVm>(DownloadManifest));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand<FixItemVm> DownloadFixCommand => downloadFixCommand ?? (downloadFixCommand = new AsyncRelayCommand<FixItemVm>(DownloadFix));

	public FixesViewModel(GabLuchiApiClient api, ManifestDownloader manifestDownloader, LuaInstaller installer, SteamService steam, SteamLibraryService library, CoverCache covers, ToastService toast, SettingsService settings)
	{
		this.api = api;
		this.manifestDownloader = manifestDownloader;
		this.installer = installer;
		this.steam = steam;
		this.library = library;
		this.covers = covers;
		this.toast = toast;
		this.settings = settings;
		InitPageSize(settings.FixesPageSize);
	}

	protected override void SavePageSizeSetting(int size)
	{
		settings.FixesPageSize = size;
	}

	protected override void OnPageSliced(IReadOnlyList<FixGameCardVm> slice)
	{
		foreach (FixGameCardVm item in slice)
		{
			item.EnsureCoverAsync(covers);
		}
	}

	public async Task LoadAsync(bool force = false)
	{
		if (!force && _allGames.Count > 0)
		{
			return;
		}
		base.IsLoading = true;
		try
		{
			DenuvoListingsResponse denuvoListingsResponse = await api.GetDenuvoListingsAsync();
			if (denuvoListingsResponse == null)
			{
				base.EmptyMessage = Strings.Fixes_Err_Load;
				return;
			}
			_allGames = denuvoListingsResponse.Games.Select((DenuvoGameListing g) => new FixGameCardVm(g)).ToList();
			Tags.Clear();
			foreach (DenuvoTag tag in denuvoListingsResponse.Tags)
			{
				Tags.Add(new TagPillVm(tag));
			}
			ApplyFilter();
			if (_allGames.Count == 0)
			{
				base.EmptyMessage = Strings.Fixes_Empty_None;
			}
		}
		catch
		{
			base.EmptyMessage = Strings.Fixes_Err_Load;
		}
		finally
		{
			base.IsLoading = false;
		}
	}

	[RelayCommand]
	private Task Refresh()
	{
		return RefreshWithCooldownAsync(async delegate
		{
			if (SearchText.Length > 0)
			{
				SearchText = "";
			}
			if (SelectedTagId != null)
			{
				SelectTag(SelectedTagId);
			}
			await LoadAsync(force: true);
			toast.Show(Strings.Fixes_Toast_Refreshed_Title, string.Format(Strings.Fixes_Toast_Refreshed_Body, _allGames.Count));
		});
	}

	[RelayCommand]
	private void SelectTag(string? tagId)
	{
		SelectedTagId = ((SelectedTagId == tagId) ? null : tagId);
		foreach (TagPillVm tag in Tags)
		{
			tag.IsSelected = tag.Id == SelectedTagId;
		}
		ApplyFilter();
	}

	private void ApplyFilter()
	{
		string q = SearchText.Trim();
		IEnumerable<FixGameCardVm> enumerable = _allGames;
		string tag = SelectedTagId;
		if (tag != null)
		{
			enumerable = enumerable.Where((FixGameCardVm g) => g.TagIds.Contains(tag));
		}
		if (q.Length > 0)
		{
			enumerable = enumerable.Where((FixGameCardVm g) => g.Matches(q));
		}
		SetFiltered(enumerable);
	}

	public async Task OpenForAppIdAsync(long appId)
	{
		if (_allGames.Count == 0)
		{
			await LoadAsync();
		}
		FixGameCardVm fixGameCardVm = _allGames.FirstOrDefault((FixGameCardVm g) => g.AppId == appId.ToString());
		if (fixGameCardVm != null)
		{
			SearchText = "";
			SelectedTagId = null;
			ApplyFilter();
			await OpenGame(fixGameCardVm);
		}
	}

	[RelayCommand]
	private async Task OpenGame(FixGameCardVm game)
	{
		SelectedGame = game;
		game.EnsureCoverAsync(covers);
		Fixes.Clear();
		_allFixes = new List<FixItemVm>();
		FixTags.Clear();
		SelectedFixTagId = null;
		IsLoadingFixes = true;
		try
		{
			DenuvoFixesResponse denuvoFixesResponse = await api.GetDenuvoFixesAsync(game.AppId);
			if (denuvoFixesResponse == null)
			{
				return;
			}
			_allFixes = denuvoFixesResponse.Fixes.Select((DenuvoFix f) => new FixItemVm(f)).ToList();
			List<DenuvoTag> list = (from t in _allFixes.SelectMany((FixItemVm f) => f.Tags)
				group t by t.Id into g
				select g.First() into t
				orderby t.Name
				select t).ToList();
			if (list.Count > 1)
			{
				foreach (DenuvoTag item in list)
				{
					FixTags.Add(new TagPillVm(item));
				}
			}
			OnPropertyChanged("HasFixTags");
			ApplyFixFilter();
		}
		catch
		{
		}
		finally
		{
			IsLoadingFixes = false;
		}
	}

	[RelayCommand]
	private void SelectFixTag(string? tagId)
	{
		SelectedFixTagId = ((SelectedFixTagId == tagId) ? null : tagId);
		foreach (TagPillVm fixTag in FixTags)
		{
			fixTag.IsSelected = fixTag.Id == SelectedFixTagId;
		}
		ApplyFixFilter();
	}

	private void ApplyFixFilter()
	{
		IEnumerable<FixItemVm> enumerable = _allFixes;
		string tag = SelectedFixTagId;
		if (tag != null)
		{
			enumerable = enumerable.Where((FixItemVm f) => f.Tags.Any((DenuvoTag t) => t.Id == tag));
		}
		Fixes.Clear();
		foreach (FixItemVm item in enumerable)
		{
			Fixes.Add(item);
		}
	}

	[RelayCommand]
	private void CloseDetail()
	{
		SelectedGame = null;
	}

	[RelayCommand]
	private Task DownloadManifest(FixItemVm fix)
	{
		return RunDownload(fix, "manifest");
	}

	[RelayCommand]
	private Task DownloadFix(FixItemVm fix)
	{
		return RunDownload(fix, "fix");
	}

	private async Task RunDownload(FixItemVm fix, string slot)
	{
		if (IsBusy)
		{
			return;
		}
		FixGameCardVm game = SelectedGame;
		if (game == null || !long.TryParse(game.AppId, out var appId))
		{
			return;
		}
		IsBusy = true;
		IsProgressIndeterminate = true;
		Progress = 0.0;
		try
		{
			string fallbackName = ((slot == "manifest") ? (fix.ManifestFilename ?? (game.AppId + ".zip")) : (fix.FixFilename ?? (game.AppId + "_fix.zip")));
			Progress<double?> progress = new Progress<double?>(delegate(double? p)
			{
				IsProgressIndeterminate = !p.HasValue;
				if (p.HasValue)
				{
					Progress = p.Value * 100.0;
				}
			});
			DownloadedFile file;
			string? localFix = FixRepository.Resolve(game.AppId, slot);
			if (localFix != null)
			{
				file = new DownloadedFile(localFix, Path.GetFileName(localFix));
			}
			else
			{
				file = await manifestDownloader.DownloadManifestAsync(game.AppId, "Ryuu", game.Name, progress);
			}
			if (slot == "manifest")
			{
				InstallManifest(file, appId, game.Name);
			}
			else
			{
				ApplyFix(file, appId, game.Name);
			}
		}
		catch (ApiException ex)
		{
			toast.Show(Strings.Fixes_Toast_DownloadFailed, ex.Message, error: true);
		}
		catch (Exception)
		{
			toast.Show(Strings.Fixes_Toast_DownloadFailed, Strings.Fixes_Toast_DownloadFailed_Body, error: true);
		}
		finally
		{
			IsBusy = false;
			IsProgressIndeterminate = false;
		}
	}

	private void InstallManifest(DownloadedFile file, long appId, string gameName)
	{
		InstallResult installResult = (file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ? installer.InstallZip(file.FilePath, appId, forceLocked: true) : installer.InstallLuaFile(file.FilePath, appId, forceLocked: true));
		DeleteStaged(file.FilePath);
		if (installResult.AnyFailed)
		{
			toast.Show(Strings.Fixes_Toast_InstallFailed, installResult.Error ?? Strings.Fixes_Toast_InstallFailed_Body, error: true);
			return;
		}
		bool flag = steam.RestartSteam();
		toast.Show(Strings.Fixes_Toast_FixInstalled, flag ? string.Format(Strings.Fixes_Toast_FixInstalled_Restarting, gameName) : string.Format(Strings.Fixes_Toast_FixInstalled_Restart, gameName));
	}

	private void ApplyFix(DownloadedFile file, long appId, string gameName)
	{
		string installDir = library.GetInstallDir(appId);
		if (installDir == null)
		{
			toast.Show(Strings.Fixes_Toast_GameNotFound, string.Format(Strings.Fixes_Toast_GameNotFound_Body, gameName), error: true);
			return;
		}
		try
		{
			using ZipArchive zipArchive = ZipFile.OpenRead(file.FilePath);
			int num = 0;
			foreach (ZipArchiveEntry entry in zipArchive.Entries)
			{
				if (!string.IsNullOrEmpty(entry.Name))
				{
					string text = Path.Combine(installDir, entry.FullName);
					try
					{
						Directory.CreateDirectory(Path.GetDirectoryName(text));
						entry.ExtractToFile(text, overwrite: true);
					}
					catch
					{
						num++;
					}
				}
			}
			if (num > 0)
			{
				toast.Show(Strings.Fixes_Toast_PartiallyApplied, string.Format(Strings.Fixes_Toast_PartiallyApplied_Body, num), error: true);
			}
			else
			{
				toast.Show(Strings.Fixes_Toast_FixApplied, string.Format(Strings.Fixes_Toast_FixApplied_Body, gameName));
			}
		}
		catch (Exception ex)
		{
			toast.Show(Strings.Fixes_Toast_CouldntApply, ex.Message, error: true);
		}
		finally
		{
			DeleteStaged(file.FilePath);
		}
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

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnSearchTextChanged(string value)
	{
		ApplyFilter();
	}
}
