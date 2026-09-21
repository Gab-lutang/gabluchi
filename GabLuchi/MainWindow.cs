using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Markup;
using GabLuchi.Resources;
using GabLuchi.Services;
using GabLuchi.ViewModels;
using GabLuchi.Views;
using Wpf.Ui.Controls;

namespace GabLuchi;

public partial class MainWindow : FluentWindow, IComponentConnector
{
	private readonly SettingsService _settings;

	private readonly LicenseService _license;

	private readonly LicenseGateViewModel _licenseGate;

	private readonly UsageService _usage;

	private readonly ToastService _toast;

	public MainWindow(MainViewModel viewModel, IServiceProvider services, SettingsService settings, LicenseService license, UsageService usage, ToastService toast)
	{
		MainWindow mainWindow = this;
		_settings = settings;
		_license = license;
		_usage = usage;
		_toast = toast;
		_licenseGate = viewModel.LicenseGate;
		InitializeComponent();
		base.DataContext = viewModel;
		RootNavigation.SetServiceProvider(services);
		base.Loaded += async delegate
		{
			RootNavigation.Navigate(typeof(HomeView));
			if (!_license.IsActivated)
			{
				_licenseGate.IsOpen = true;
			}
			try
			{
				await viewModel.InitializeAsync();
			}
			catch
			{
			}
		};
	}

	private bool NavigateOrGate(Type pageType)
	{
		if (_license.IsActivated)
		{
			RootNavigation.Navigate(pageType);
			return true;
		}
		_licenseGate.IsOpen = true;
		return false;
	}

	public void ShowAndActivate()
	{
		Show();
		base.WindowState = WindowState.Normal;
		Activate();
		base.Topmost = true;
		base.Topmost = false;
		Focus();
	}

	public void NavigateToAdd()
	{
		NavigateOrGate(typeof(DownloadView));
	}

	public void NavigateToManage()
	{
		NavigateOrGate(typeof(ManageView));
	}

	public void NavigateToSettings()
	{
		RootNavigation.Navigate(typeof(SettingsView));
	}

	public void NavigateToFixes()
	{
		NavigateOrGate(typeof(FixesView));
	}

	public void NavigateToPlugin()
	{
		NavigateOrGate(typeof(PluginView));
	}

	public void NavigateToMode()
	{
		NavigateOrGate(typeof(ModeView));
	}

	private void NavAdd_Click(object sender, RoutedEventArgs e) => NavigateToAdd();

	private void NavManage_Click(object sender, RoutedEventArgs e) => NavigateToManage();

	private void NavMode_Click(object sender, RoutedEventArgs e) => NavigateToMode();

	private void NavFixes_Click(object sender, RoutedEventArgs e)
	{
		if (_usage.IsFreeTier && !_usage.IsExpired)
		{
			_toast.Show("Paid Feature", "Fixes require a paid key. Upgrade for full access.", error: true);
			return;
		}
		NavigateToFixes();
	}

	private void NavMultiplayerFix_Click(object sender, RoutedEventArgs e) => NavigateOrGate(typeof(MultiplayerFixView));

	private void NavPlugin_Click(object sender, RoutedEventArgs e) => NavigateToPlugin();

	private void NavDlcUnlocker_Click(object sender, RoutedEventArgs e)
	{
		if (_usage.IsFreeTier && !_usage.IsExpired)
		{
			_toast.Show("Paid Feature", "DLC Unlocker requires a paid key. Upgrade for full access.", error: true);
			return;
		}
		NavigateOrGate(typeof(DlcUnlockerView));
	}

	private void RestartSteam_Click(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is MainViewModel mainViewModel)
		{
			mainViewModel.RestartSteamCommand.Execute(null);
		}
	}
}
