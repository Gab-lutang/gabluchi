using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using GabLuchi.Models;

namespace GabLuchi.ViewModels;

public class SourceRowViewModel : ObservableObject
{
	private readonly DownloadViewModel _parent;

	[ObservableProperty]
	[NotifyPropertyChangedFor("CanDownload")]
	private bool _isLocked;

	[ObservableProperty]
	private string? _statsText;

	[ObservableProperty]
	private bool _isSupporter;

	[ObservableProperty]
	private bool _isDownloading;

	[ObservableProperty]
	private double _progress;

	[ObservableProperty]
	private bool _isProgressIndeterminate;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? downloadCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? openDiscordCommand;

	public string Name { get; }

	public string DisplayName { get; }

	public string Status { get; }

	public string? DiscordUrl { get; }

	public bool NeedsKey { get; }

	public bool IsAvailable => Status == "available";

	public string StatusLabel => Status.ToUpperInvariant();

	public bool ShowStatus => Status != "unknown";

	public bool CanDownload
	{
		get
		{
			if (IsAvailable)
			{
				return !IsLocked;
			}
			return false;
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsLocked
	{
		get
		{
			return _isLocked;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isLocked, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsLocked);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.CanDownload);
				_isLocked = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsLocked);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.CanDownload);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string? StatsText
	{
		get
		{
			return _statsText;
		}
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_statsText, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.StatsText);
				_statsText = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.StatsText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsSupporter
	{
		get
		{
			return _isSupporter;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isSupporter, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsSupporter);
				_isSupporter = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsSupporter);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsDownloading
	{
		get
		{
			return _isDownloading;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isDownloading, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsDownloading);
				_isDownloading = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsDownloading);
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
	public IAsyncRelayCommand DownloadCommand => downloadCommand ?? (downloadCommand = new AsyncRelayCommand(DownloadAsync));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand OpenDiscordCommand => openDiscordCommand ?? (openDiscordCommand = new RelayCommand(OpenDiscord));

	public SourceRowViewModel(DownloadViewModel parent, string name, string status)
	{
		_parent = parent;
		Name = name;
		Status = status;
		SourceMeta.Meta meta = SourceMeta.Get(name);
		DisplayName = meta.DisplayName ?? name;
		DiscordUrl = meta.DiscordUrl;
		NeedsKey = meta.RequiresUserKey;
	}

	[RelayCommand]
	private Task DownloadAsync()
	{
		return _parent.DownloadFromSourceAsync(this);
	}

	[RelayCommand]
	private void OpenDiscord()
	{
		if (DiscordUrl != null)
		{
			Process.Start(new ProcessStartInfo(DiscordUrl)
			{
				UseShellExecute = true
			});
		}
	}
}
