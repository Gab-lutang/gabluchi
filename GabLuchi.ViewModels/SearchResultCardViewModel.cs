using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using GabLuchi.Models;
using GabLuchi.Services;

namespace GabLuchi.ViewModels;

public class SearchResultCardViewModel : ObservableObject
{
	private int _resolving;

	private string? _cover;

	public SearchResultCardViewModel(SteamSearchResult result)
	{
		Result = result;
	}

	public SteamSearchResult Result { get; }

	public long AppId => Result.AppId;

	public string Name => Result.Name;

	public bool HasCover => Cover != null;

	public string? Cover
	{
		get
		{
			return _cover;
		}
		private set
		{
			if (_cover == value)
			{
				return;
			}
			_cover = value;
			OnPropertyChanged(nameof(Cover));
			OnPropertyChanged(nameof(HasCover));
		}
	}

	public async Task EnsureCoverAsync(SteamAppInfoCache appInfo, CoverCache covers)
	{
		if (Cover != null || Interlocked.Exchange(ref _resolving, 1) == 1)
		{
			return;
		}
		try
		{
			string? local = covers.GetLocalPath(AppId);
			if (local == null && !covers.IsKnownMissing(AppId))
			{
				local = await covers.EnsureAsync(AppId, SteamAppInfoCache.GuessHeaderImageUrl(AppId));
			}
			if (local == null)
			{
				SteamAppInfo? info = appInfo.GetCached(AppId);
				if (info == null)
				{
					info = await appInfo.ResolveAsync(AppId);
				}
				if (!string.IsNullOrWhiteSpace(info?.HeaderImage))
				{
					local = await covers.EnsureAsync(AppId, info.HeaderImage);
				}
				if (local == null && info != null)
				{
					covers.MarkMissing(AppId);
				}
			}
			if (local != null)
			{
				Cover = local;
			}
		}
		finally
		{
			Interlocked.Exchange(ref _resolving, 0);
		}
	}
}