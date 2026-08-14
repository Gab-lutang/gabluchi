using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using GabLuchi.Resources;

namespace GabLuchi.ViewModels;

public abstract class PagedListViewModel<T> : ObservableObject
{
	protected List<T> _filtered = new List<T>();

	private DateTime _lastRefresh;

	private int _filteredCount;

	private bool _suppressPageSlice;

	[ObservableProperty]
	[NotifyPropertyChangedFor("HasItems")]
	[NotifyPropertyChangedFor("ShowItems")]
	[NotifyPropertyChangedFor("IsEmpty")]
	private ObservableCollection<T> _items = new ObservableCollection<T>();

	[ObservableProperty]
	[NotifyPropertyChangedFor("HasItems")]
	[NotifyPropertyChangedFor("ShowItems")]
	[NotifyPropertyChangedFor("IsEmpty")]
	private bool _isLoading;

	[ObservableProperty]
	private string _emptyMessage = "";

	public const string AllPages = "All";

	[ObservableProperty]
	private string _selectedPageSize = "24";

	[ObservableProperty]
	[NotifyPropertyChangedFor("CanGoPrev")]
	[NotifyPropertyChangedFor("CanGoNext")]
	[NotifyPropertyChangedFor("PageLabel")]
	private int _currentPage = 1;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? prevPageCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? nextPageCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand<int>? goToPageCommand;

	public Action? ScrollToTop { get; set; }

	public bool HasItems => Items.Count > 0;

	public bool ShowItems
	{
		get
		{
			if (HasItems)
			{
				return !IsLoading;
			}
			return false;
		}
	}

	public bool IsEmpty
	{
		get
		{
			if (!IsLoading)
			{
				return Items.Count == 0;
			}
			return false;
		}
	}

	public ObservableCollection<string> PageSizeOptions { get; } = new ObservableCollection<string> { "12", "24", "48", "All" };

	public int PageSize
	{
		get
		{
			if (!(SelectedPageSize == "All"))
			{
				return int.Parse(SelectedPageSize);
			}
			return 0;
		}
	}

	public int TotalPages
	{
		get
		{
			if (PageSize != 0)
			{
				return Math.Max(1, (int)Math.Ceiling((double)_filteredCount / (double)PageSize));
			}
			return 1;
		}
	}

	public ObservableCollection<int> PageNumbers { get; } = new ObservableCollection<int>();

	public bool CanGoPrev => CurrentPage > 1;

	public bool CanGoNext => CurrentPage < TotalPages;

	public bool ShowPager
	{
		get
		{
			if (PageSize != 0)
			{
				return TotalPages > 1;
			}
			return false;
		}
	}

	public string PageLabel => string.Format(Strings.Manage_PageLabel, CurrentPage, TotalPages);

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public ObservableCollection<T> Items
	{
		get
		{
			return _items;
		}
		[MemberNotNull("_items")]
		set
		{
			if (!EqualityComparer<ObservableCollection<T>>.Default.Equals(_items, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Items);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HasItems);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ShowItems);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsEmpty);
				_items = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Items);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HasItems);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ShowItems);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsEmpty);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsLoading
	{
		get
		{
			return _isLoading;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isLoading, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsLoading);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HasItems);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ShowItems);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsEmpty);
				_isLoading = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsLoading);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HasItems);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ShowItems);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsEmpty);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string EmptyMessage
	{
		get
		{
			return _emptyMessage;
		}
		[MemberNotNull("_emptyMessage")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_emptyMessage, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.EmptyMessage);
				_emptyMessage = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.EmptyMessage);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string SelectedPageSize
	{
		get
		{
			return _selectedPageSize;
		}
		[MemberNotNull("_selectedPageSize")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_selectedPageSize, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.SelectedPageSize);
				_selectedPageSize = value;
				OnSelectedPageSizeChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.SelectedPageSize);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public int CurrentPage
	{
		get
		{
			return _currentPage;
		}
		set
		{
			if (!EqualityComparer<int>.Default.Equals(_currentPage, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CurrentPage);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CanGoPrev);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CanGoNext);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.PageLabel);
				_currentPage = value;
				OnCurrentPageChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CurrentPage);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CanGoPrev);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CanGoNext);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.PageLabel);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand PrevPageCommand => prevPageCommand ?? (prevPageCommand = new RelayCommand(PrevPage));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand NextPageCommand => nextPageCommand ?? (nextPageCommand = new RelayCommand(NextPage));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<int> GoToPageCommand => goToPageCommand ?? (goToPageCommand = new RelayCommand<int>(GoToPage));

	[RelayCommand]
	private void PrevPage()
	{
		if (CanGoPrev)
		{
			CurrentPage--;
		}
	}

	[RelayCommand]
	private void NextPage()
	{
		if (CanGoNext)
		{
			CurrentPage++;
		}
	}

	[RelayCommand]
	private void GoToPage(int page)
	{
		if (page >= 1 && page <= TotalPages && page != CurrentPage)
		{
			CurrentPage = page;
		}
	}

	protected void InitPageSize(int persisted)
	{
		_selectedPageSize = ((persisted == 0) ? "All" : persisted.ToString());
	}

	protected virtual void SavePageSizeSetting(int size)
	{
	}

	protected virtual void OnPageSliced(IReadOnlyList<T> slice)
	{
	}

	protected void SetFiltered(IEnumerable<T> filtered, bool resetPage = true)
	{
		_suppressPageSlice = true;
		if (resetPage)
		{
			CurrentPage = 1;
		}
		_filtered = (filtered as List<T>) ?? filtered.ToList();
		_filteredCount = _filtered.Count;
		OnPropertyChanged("TotalPages");
		OnPropertyChanged("ShowPager");
		if (CurrentPage > TotalPages)
		{
			CurrentPage = TotalPages;
		}
		_suppressPageSlice = false;
		ApplyPageSlice();
	}

	private void ApplyPageSlice()
	{
		List<T> list = ((PageSize == 0) ? _filtered : _filtered.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList());
		Items = new ObservableCollection<T>(list);
		RebuildPageNumbers();
		OnPropertyChanged("CanGoPrev");
		OnPropertyChanged("CanGoNext");
		OnPropertyChanged("PageLabel");
		ScrollToTop?.Invoke();
		OnPageSliced(list);
	}

	private void RebuildPageNumbers()
	{
		PageNumbers.Clear();
		int totalPages = TotalPages;
		int currentPage = CurrentPage;
		if (PageSize == 0 || totalPages <= 1)
		{
			return;
		}
		if (totalPages <= 9)
		{
			for (int i = 1; i <= totalPages; i++)
			{
				Add(i);
			}
			return;
		}
		Add(1);
		int num = Math.Max(2, currentPage - 1);
		int num2 = Math.Min(totalPages - 1, currentPage + 1);
		if (num > 2)
		{
			Add(0);
		}
		for (int j = num; j <= num2; j++)
		{
			Add(j);
		}
		if (num2 < totalPages - 1)
		{
			Add(0);
		}
		Add(totalPages);
		void Add(int n)
		{
			PageNumbers.Add(n);
		}
	}

	protected async Task RefreshWithCooldownAsync(Func<Task> reload)
	{
		if (!(DateTime.UtcNow - _lastRefresh < TimeSpan.FromSeconds(1.0)) && !IsLoading)
		{
			_lastRefresh = DateTime.UtcNow;
			await reload();
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnSelectedPageSizeChanged(string value)
	{
		SavePageSizeSetting(PageSize);
		_suppressPageSlice = true;
		CurrentPage = 1;
		OnPropertyChanged("TotalPages");
		OnPropertyChanged("ShowPager");
		_suppressPageSlice = false;
		ApplyPageSlice();
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnCurrentPageChanged(int value)
	{
		if (!_suppressPageSlice)
		{
			ApplyPageSlice();
		}
	}
}
