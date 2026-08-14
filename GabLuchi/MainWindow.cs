using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
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

	private NotifyIcon? _trayIcon;

	private bool _reallyExiting;

	public MainWindow(MainViewModel viewModel, IServiceProvider services, SettingsService settings)
	{
		MainWindow mainWindow = this;
		_settings = settings;
		InitializeComponent();
		base.DataContext = viewModel;
		RootNavigation.SetServiceProvider(services);
		InitializeTrayIcon();
		base.Closing += OnWindowClosing;
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

	private void InitializeTrayIcon()
	{
		_trayIcon = new NotifyIcon
		{
			Text = "GabLuchi",
			Visible = false
		};
		try
		{
			using Stream stream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/icon.ico"))?.Stream;
			if (stream != null)
			{
				_trayIcon.Icon = new Icon(stream);
			}
		}
		catch
		{
		}
		_trayIcon.DoubleClick += delegate
		{
			RestoreFromTray();
		};
		ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
		contextMenuStrip.Items.Add(Strings.Tray_Open, null, delegate
		{
			RestoreFromTray();
		});
		contextMenuStrip.Items.Add(new ToolStripSeparator());
		contextMenuStrip.Items.Add(Strings.Tray_Exit, null, delegate
		{
			ExitApp();
		});
		_trayIcon.ContextMenuStrip = contextMenuStrip;
	}

	private void OnWindowClosing(object? sender, CancelEventArgs e)
	{
		if (!_reallyExiting && (_settings.MinimizeToTray || Program.SessionTrayLock))
		{
			e.Cancel = true;
			Hide();
			if (_trayIcon != null)
			{
				_trayIcon.Visible = true;
			}
		}
		else
		{
			_trayIcon?.Dispose();
			System.Windows.Application.Current.Shutdown();
		}
	}

	private void ExitApp()
	{
		_reallyExiting = true;
		if (_trayIcon != null)
		{
			_trayIcon.Visible = false;
		}
		Close();
	}

	public void RestoreFromTray()
	{
		Show();
		base.WindowState = WindowState.Normal;
		if (_trayIcon != null)
		{
			_trayIcon.Visible = false;
		}
		Activate();
		base.Topmost = true;
		base.Topmost = false;
		Focus();
	}

	public void StartSilent()
	{
		if (_trayIcon != null)
		{
			_trayIcon.Visible = true;
		}
	}

	public void ShowInstallNotification(string message, bool error)
	{
		if (_trayIcon != null)
		{
			_trayIcon.Visible = true;
			_trayIcon.ShowBalloonTip(5000, "GabLuchi", message, (!error) ? ToolTipIcon.Info : ToolTipIcon.Error);
		}
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

	private void RestartSteam_Click(object sender, RoutedEventArgs e)
	{
		if (base.DataContext is MainViewModel mainViewModel)
		{
			mainViewModel.RestartSteamCommand.Execute(null);
		}
	}
}
