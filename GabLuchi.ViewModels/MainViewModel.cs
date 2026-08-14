using System;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GabLuchi.Resources;
using GabLuchi.Services;

namespace GabLuchi.ViewModels;

public class MainViewModel : ObservableObject
{
	private readonly SteamService _steam;

	private readonly AuthService _auth;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand? restartSteamCommand;

	public OnboardingViewModel Onboarding { get; }

	public string VersionLabel { get; } = "v" + ReadVersion();

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand RestartSteamCommand => restartSteamCommand ?? (restartSteamCommand = new RelayCommand(RestartSteam));

	private static string ReadVersion()
	{
		string text = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "?";
		int num = text.IndexOf('+');
		if (num < 0)
		{
			return text;
		}
		return text.Substring(0, num);
	}

	public MainViewModel(SteamService steam, AuthService auth, OnboardingViewModel onboarding)
	{
		_steam = steam;
		_auth = auth;
		Onboarding = onboarding;
	}

	public async Task InitializeAsync()
	{
		await _auth.InitializeAsync();
	}

	[RelayCommand]
	private void RestartSteam()
	{
		if (MessageBox.Show(Strings.Main_RestartSteam_Ask, Strings.Manage_RestartSteam_Title, MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK && !_steam.RestartSteam())
		{
			MessageBox.Show(Strings.Manage_RestartSteam_Failed, Strings.Manage_RestartSteam_Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
		}
	}
}
