using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GabLuchi.Resources;
using GabLuchi.Services;

namespace GabLuchi.ViewModels;

public class MainViewModel : ObservableObject
{
	private readonly SteamService _steam;

	private readonly AuthService _auth;

	private readonly AnalyticsService _analytics;

	private readonly AnnouncementService _announcement;

	private RelayCommand? restartSteamCommand;

	private RelayCommand? dismissAnnouncementCommand;

	public OnboardingViewModel Onboarding { get; }

	public LicenseGateViewModel LicenseGate { get; }

	public string VersionLabel { get; } = "v" + ReadVersion();

	private string? _announcementText;
	public string? AnnouncementText
	{
		get => _announcementText;
		set => SetProperty(ref _announcementText, value);
	}

	private Visibility _announcementVisibility = Visibility.Collapsed;
	public Visibility AnnouncementVisibility
	{
		get => _announcementVisibility;
		set => SetProperty(ref _announcementVisibility, value);
	}

	public bool HasAnnouncement => !string.IsNullOrWhiteSpace(AnnouncementText) && AnnouncementVisibility == Visibility.Visible;

	public IRelayCommand RestartSteamCommand => restartSteamCommand ?? (restartSteamCommand = new RelayCommand(RestartSteam));

	public IRelayCommand DismissAnnouncementCommand => dismissAnnouncementCommand ?? (dismissAnnouncementCommand = new RelayCommand(DismissAnnouncement));

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

	public MainViewModel(SteamService steam, AuthService auth, AnalyticsService analytics, OnboardingViewModel onboarding, LicenseGateViewModel licenseGate, AnnouncementService announcement)
	{
		_steam = steam;
		_auth = auth;
		_analytics = analytics;
		Onboarding = onboarding;
		LicenseGate = licenseGate;
		_announcement = announcement;
		_announcement.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(AnnouncementService.Announcement))
			{
				AnnouncementText = _announcement.Announcement;
				AnnouncementVisibility = string.IsNullOrWhiteSpace(_announcement.Announcement) || _announcement.IsDismissed ? Visibility.Collapsed : Visibility.Visible;
				OnPropertyChanged(nameof(HasAnnouncement));
			}
			if (e.PropertyName == nameof(AnnouncementService.IsDismissed))
			{
				AnnouncementVisibility = _announcement.IsDismissed ? Visibility.Collapsed : Visibility.Visible;
				OnPropertyChanged(nameof(HasAnnouncement));
			}
		};
	}

	public async Task InitializeAsync()
	{
		await _auth.InitializeAsync();
		_ = _analytics.TrackAppLaunchAsync();
		_ = _analytics.TrackInstalledGamesAsync();
	}

	private void DismissAnnouncement()
	{
		_announcement.Dismiss();
		AnnouncementVisibility = Visibility.Collapsed;
		OnPropertyChanged(nameof(HasAnnouncement));
	}

	private void RestartSteam()
	{
		if (MessageBox.Show(Strings.Main_RestartSteam_Ask, Strings.Manage_RestartSteam_Title, MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK && !_steam.RestartSteam())
		{
			MessageBox.Show(Strings.Manage_RestartSteam_Failed, Strings.Manage_RestartSteam_Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
		}
	}
}
