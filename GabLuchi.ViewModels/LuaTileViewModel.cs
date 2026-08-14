using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using GabLuchi.Services;

namespace GabLuchi.ViewModels;

public class LuaTileViewModel : ObservableObject
{
	[ObservableProperty]
	[NotifyPropertyChangedFor("AddedReleaseLabel")]
	private string _releaseLabel = "";

	private int _resolving;

	private bool _nameIsPlaceholder;

	[ObservableProperty]
	private string _name;

	[ObservableProperty]
	private ImageSource? _cover;

	[ObservableProperty]
	private bool _isSelected;

	[ObservableProperty]
	private bool _detailsLoaded;

	public long AppId { get; }

	public string FilePath { get; }

	public DateTime AddedAt { get; }

	public string AddedLabel => "Added " + AddedAt.ToString("MMM d, \\'yy", CultureInfo.InvariantCulture);

	public string AddedReleaseLabel
	{
		get
		{
			if (!string.IsNullOrEmpty(ReleaseLabel))
			{
				return AddedLabel + "  •  " + ReleaseLabel;
			}
			return AddedLabel;
		}
	}

	public Action? SelectionChanged { get; set; }

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string ReleaseLabel
	{
		get
		{
			return _releaseLabel;
		}
		[MemberNotNull("_releaseLabel")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_releaseLabel, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ReleaseLabel);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.AddedReleaseLabel);
				_releaseLabel = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ReleaseLabel);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.AddedReleaseLabel);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string Name
	{
		get
		{
			return _name;
		}
		[MemberNotNull("_name")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_name, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Name);
				_name = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Name);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public ImageSource? Cover
	{
		get
		{
			return _cover;
		}
		set
		{
			if (!EqualityComparer<ImageSource>.Default.Equals(_cover, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Cover);
				_cover = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Cover);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isSelected, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsSelected);
				_isSelected = value;
				OnIsSelectedChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsSelected);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool DetailsLoaded
	{
		get
		{
			return _detailsLoaded;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_detailsLoaded, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.DetailsLoaded);
				_detailsLoaded = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.DetailsLoaded);
			}
		}
	}

	public void UpdateReleaseLabel(SteamAppInfoCache appInfo)
	{
		string text = appInfo.GetFilterData(AppId)?.ReleaseDateText;
		if (string.IsNullOrWhiteSpace(text))
		{
			ReleaseLabel = "";
			return;
		}
		text = Regex.Replace(text, "\\b(19|20)(\\d{2})\\b", "'$2");
		ReleaseLabel = "Released " + text;
	}

	public LuaTileViewModel(long appId, string filePath, DateTime addedAt, string name, bool nameIsPlaceholder)
	{
		AppId = appId;
		FilePath = filePath;
		AddedAt = addedAt;
		_name = name;
		_nameIsPlaceholder = nameIsPlaceholder;
	}

	public async Task EnsureResolvedAsync(SteamAppInfoCache appInfo, CoverCache covers)
	{
		if (string.IsNullOrEmpty(ReleaseLabel))
		{
			OnUi(delegate
			{
				UpdateReleaseLabel(appInfo);
			});
		}
		if (Cover != null || Interlocked.Exchange(ref _resolving, 1) == 1)
		{
			return;
		}
		try
		{
			string local = await ResolveCoverFileAsync(AppId, appInfo, covers, delegate(string name)
			{
				if (_nameIsPlaceholder)
				{
					SetName(name);
				}
			});
			if (_nameIsPlaceholder)
			{
				SteamAppInfo steamAppInfo = appInfo.GetCached(AppId);
				if ((object)steamAppInfo == null)
				{
					steamAppInfo = await appInfo.ResolveAsync(AppId);
				}
				SteamAppInfo steamAppInfo2 = steamAppInfo;
				if (!string.IsNullOrWhiteSpace(steamAppInfo2?.Name))
				{
					SetName(steamAppInfo2.Name);
				}
			}
			if (local == null)
			{
				return;
			}
			ImageSource image = await Task.Run(() => LoadFrozen(local));
			if (image != null)
			{
				OnUi(delegate
				{
					Cover = image;
				});
			}
		}
		finally
		{
			Interlocked.Exchange(ref _resolving, 0);
		}
	}

	public static async Task<string?> ResolveCoverFileAsync(long appId, SteamAppInfoCache appInfo, CoverCache covers, Action<string>? onName = null)
	{
		string local = covers.GetLocalPath(appId);
		if (local != null)
		{
			return local;
		}
		if (covers.IsKnownMissing(appId))
		{
			return null;
		}
		local = await covers.EnsureAsync(appId, SteamAppInfoCache.GuessHeaderImageUrl(appId));
		if (local != null)
		{
			return local;
		}
		SteamAppInfo steamAppInfo = appInfo.GetCached(appId);
		if ((object)steamAppInfo == null)
		{
			steamAppInfo = await appInfo.ResolveAsync(appId);
		}
		SteamAppInfo info = steamAppInfo;
		if (onName != null && !string.IsNullOrWhiteSpace(info?.Name))
		{
			onName(info.Name);
		}
		if (!string.IsNullOrWhiteSpace(info?.HeaderImage))
		{
			local = await covers.EnsureAsync(appId, info.HeaderImage);
		}
		if (local == null && (object)info != null)
		{
			covers.MarkMissing(appId);
		}
		return local;
	}

	private static ImageSource? LoadFrozen(string path)
	{
		try
		{
			BitmapImage bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage.DecodePixelWidth = 248;
			bitmapImage.UriSource = new Uri(path, UriKind.Absolute);
			bitmapImage.EndInit();
			((Freezable)bitmapImage).Freeze();
			return bitmapImage;
		}
		catch
		{
			return null;
		}
	}

	private void SetName(string name)
	{
		_nameIsPlaceholder = false;
		OnUi(delegate
		{
			Name = name;
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

	public bool Matches(string q)
	{
		if (!Name.Contains(q, StringComparison.OrdinalIgnoreCase))
		{
			return AppId.ToString().Contains(q);
		}
		return true;
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnIsSelectedChanged(bool value)
	{
		SelectionChanged?.Invoke();
	}
}
