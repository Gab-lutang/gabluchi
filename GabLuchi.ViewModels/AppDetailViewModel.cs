using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using GabLuchi.Resources;

namespace GabLuchi.ViewModels;

public class AppDetailViewModel : ObservableObject
{
	[ObservableProperty]
	private bool _isLoading = true;

	[ObservableProperty]
	private string? _error;

	[ObservableProperty]
	private IReadOnlyList<DepotRow> _inLua = Array.Empty<DepotRow>();

	[ObservableProperty]
	private IReadOnlyList<DepotRow> _missing = Array.Empty<DepotRow>();

	[ObservableProperty]
	private IReadOnlyList<DepotRow> _unknown = Array.Empty<DepotRow>();

	[ObservableProperty]
	[NotifyPropertyChangedFor("InLuaToggleLabel")]
	private bool _isInLuaExpanded = true;

	[ObservableProperty]
	[NotifyPropertyChangedFor("MissingToggleLabel")]
	private bool _isMissingExpanded = true;

	[ObservableProperty]
	[NotifyPropertyChangedFor("UnknownToggleLabel")]
	private bool _isUnknownExpanded;

	public long AppId { get; }

	public int InLuaCount => InLua.Count;

	public int MissingCount => Missing.Count;

	public int UnknownCount => Unknown.Count;

	public bool HasInLua => InLua.Count > 0;

	public bool HasMissing => Missing.Count > 0;

	public bool HasUnknown => Unknown.Count > 0;

	public string InLuaToggleLabel => (IsInLuaExpanded ? "▾" : "▸") + " " + Strings.Manage_Toggle_InLua;

	public string MissingToggleLabel => (IsMissingExpanded ? "▾" : "▸") + " " + Strings.Manage_Toggle_Missing;

	public string UnknownToggleLabel => (IsUnknownExpanded ? "▾" : "▸") + " " + Strings.Manage_Toggle_Unknown;

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
				_isLoading = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsLoading);
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
	public IReadOnlyList<DepotRow> InLua
	{
		get
		{
			return _inLua;
		}
		[MemberNotNull("_inLua")]
		set
		{
			if (!EqualityComparer<IReadOnlyList<DepotRow>>.Default.Equals(_inLua, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.InLua);
				_inLua = value;
				OnInLuaChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.InLua);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IReadOnlyList<DepotRow> Missing
	{
		get
		{
			return _missing;
		}
		[MemberNotNull("_missing")]
		set
		{
			if (!EqualityComparer<IReadOnlyList<DepotRow>>.Default.Equals(_missing, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Missing);
				_missing = value;
				OnMissingChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Missing);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IReadOnlyList<DepotRow> Unknown
	{
		get
		{
			return _unknown;
		}
		[MemberNotNull("_unknown")]
		set
		{
			if (!EqualityComparer<IReadOnlyList<DepotRow>>.Default.Equals(_unknown, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Unknown);
				_unknown = value;
				OnUnknownChanged(value);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Unknown);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsInLuaExpanded
	{
		get
		{
			return _isInLuaExpanded;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isInLuaExpanded, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsInLuaExpanded);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.InLuaToggleLabel);
				_isInLuaExpanded = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsInLuaExpanded);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.InLuaToggleLabel);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsMissingExpanded
	{
		get
		{
			return _isMissingExpanded;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isMissingExpanded, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsMissingExpanded);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.MissingToggleLabel);
				_isMissingExpanded = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsMissingExpanded);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.MissingToggleLabel);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsUnknownExpanded
	{
		get
		{
			return _isUnknownExpanded;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isUnknownExpanded, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsUnknownExpanded);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.UnknownToggleLabel);
				_isUnknownExpanded = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsUnknownExpanded);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.UnknownToggleLabel);
			}
		}
	}

	public AppDetailViewModel(long appId)
	{
		AppId = appId;
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnInLuaChanged(IReadOnlyList<DepotRow> value)
	{
		OnPropertyChanged("InLuaCount");
		OnPropertyChanged("HasInLua");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnMissingChanged(IReadOnlyList<DepotRow> value)
	{
		OnPropertyChanged("MissingCount");
		OnPropertyChanged("HasMissing");
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	private void OnUnknownChanged(IReadOnlyList<DepotRow> value)
	{
		OnPropertyChanged("UnknownCount");
		OnPropertyChanged("HasUnknown");
	}
}
