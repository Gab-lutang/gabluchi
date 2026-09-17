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

	public MainWindow(MainViewModel viewModel, IServiceProvider services, SettingsService settings)
	{
		MainWindow mainWindow = this;
		_settings = settings;
		InitializeComponent();
		base.DataContext = viewModel;
		RootNavigation.SetServiceProvider(services);
		base.Loaded += async delegate
		{
			mainWindow.RootNavigation.Navigate(typeof(HomeView));
			try
			{
				await viewModel.InitializeAsync();
			}
			catch
			{
			}
		};
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
		RootNavigation.Navigate(typeof(DownloadView));
	}

	public void NavigateToManage()
	{
		RootNavigation.Navigate(typeof(ManageView));
	}

	public void NavigateToSettings()
	{
		RootNavigation.Navigate(typeof(SettingsView));
	}

	public void NavigateToFixes()
	{
		RootNavigation.Navigate(typeof(FixesView));
	}

	public void NavigateToPlugin()
	{
		RootNavigation.Navigate(typeof(PluginView));
	}

	public void NavigateToMode()
	{
		RootNavigation.Navigate(typeof(ModeView));
	}

	// GameHealth: hidden, feature not released yet
	// public void NavigateToHealth()
	// {
	// 	RootNavigation.Navigate(typeof(GameHealthView));
	// }

	// SmartDlc: hidden, feature not released yet
	// public void NavigateToSmartDlc()
	// {
	// 	RootNavigation.Navigate(typeof(SmartDlcView));
	// }

	private void RestartSteam_Click(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is MainViewModel mainViewModel)
		{
			mainViewModel.RestartSteamCommand.Execute(null);
		}
	}
}
