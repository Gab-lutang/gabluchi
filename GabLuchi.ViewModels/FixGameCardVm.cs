using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using GabLuchi.Models;
using GabLuchi.Resources;
using GabLuchi.Services;

namespace GabLuchi.ViewModels;

public class FixGameCardVm(DenuvoGameListing g) : ObservableObject
{
	[ObservableProperty]
	private string? _cover;

	private int _resolving;

	public string AppId { get; } = g.AppId;

	public string Name { get; } = g.Name;

	public string? HeaderImage { get; } = g.HeaderImage;

	public int FixCount { get; } = g.FixCount;

	public IReadOnlyList<string> TagIds { get; } = g.Tags.Select((DenuvoTag t) => t.Id).ToList();

	public string FixCountLabel => string.Format(Strings.Fixes_Count, FixCount);

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string? Cover
	{
		get
		{
			return _cover;
		}
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_cover, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Cover);
				_cover = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Cover);
			}
		}
	}

	public bool Matches(string q)
	{
		if (!Name.Contains(q, StringComparison.OrdinalIgnoreCase))
		{
			return AppId.Contains(q);
		}
		return true;
	}

	public async Task EnsureCoverAsync(CoverCache covers)
	{
		if (Cover != null || string.IsNullOrWhiteSpace(HeaderImage) || !long.TryParse(AppId, out var result) || Interlocked.Exchange(ref _resolving, 1) == 1)
		{
			return;
		}
		try
		{
			string text = covers.GetLocalPath(result);
			if (text == null)
			{
				text = await covers.EnsureAsync(result, HeaderImage);
			}
			string text2 = text;
			if (text2 != null)
			{
				Cover = text2;
			}
		}
		finally
		{
			Interlocked.Exchange(ref _resolving, 0);
		}
	}
}
