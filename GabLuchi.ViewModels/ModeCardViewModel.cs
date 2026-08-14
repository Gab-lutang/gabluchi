using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using GabLuchi.Models;
using GabLuchi.Resources;

namespace GabLuchi.ViewModels;

public class ModeCardViewModel(UnlockerMode mode, string title, string description) : ObservableObject
{
	[ObservableProperty]
	private string _statusText = Strings.Mode_Checking;

	[ObservableProperty]
	private string _buttonText = Strings.Mode_Btn_Install;

	[ObservableProperty]
	[NotifyPropertyChangedFor("ShowManage")]
	private bool _isActive;

	public UnlockerMode Mode { get; } = mode;

	public string Title { get; } = title;

	public string Description { get; } = description;

	public bool IsCloudRedirect => Mode == UnlockerMode.CloudRedirect;

	public bool ShowManage
	{
		get
		{
			if (IsCloudRedirect)
			{
				return IsActive;
			}
			return false;
		}
	}

	public bool IsRecommended => Mode == UnlockerMode.OpenSteamTools;

	public bool IsExperimental => Mode == UnlockerMode.OpenSteamToolsNightly;

	public bool SupportsCloudRedirect => Mode == UnlockerMode.OpenSteamToolsNightly;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string StatusText
	{
		get
		{
			return _statusText;
		}
		[MemberNotNull("_statusText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_statusText, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.StatusText);
				_statusText = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.StatusText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string ButtonText
	{
		get
		{
			return _buttonText;
		}
		[MemberNotNull("_buttonText")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_buttonText, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ButtonText);
				_buttonText = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ButtonText);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsActive
	{
		get
		{
			return _isActive;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isActive, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsActive);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ShowManage);
				_isActive = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsActive);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ShowManage);
			}
		}
	}
}
