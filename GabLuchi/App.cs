using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using GabLuchi.Models;
using GabLuchi.Resources;
using GabLuchi.Services;
using GabLuchi.ViewModels;
using GabLuchi.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GabLuchi;

public partial class App : Application
{
	private readonly IHost _host;

	private bool _exitAfterSilentInstall;

	private readonly SemaphoreSlim _updateFlowGate = new SemaphoreSlim(1, 1);

	internal static Func<Task>? RunUpdateFlow;

	private UpdateService Updates => _host.Services.GetRequiredService<UpdateService>();

	public App()
	{
		_host = new HostBuilder()
			.ConfigureLogging(delegate(ILoggingBuilder logging)
			{
				logging.AddDebug();
			})
			.ConfigureServices(delegate(IServiceCollection services)
		{
			services.AddSingleton<SettingsService>();
			services.AddSingleton<LicenseService>();
			services.AddSingleton<CacheService>();
			services.AddSingleton<AuthService>();
			services.AddSingleton<SteamService>();
			services.AddSingleton<SteamAppListCache>();
			services.AddSingleton<SteamAppInfoCache>();
			services.AddSingleton<CoverCache>();
			services.AddSingleton<ToastService>();
			services.AddSingleton<SteamDepotInfo>();
			services.AddSingleton<LuaInstaller>();
			services.AddSingleton<SteamLibraryService>();
			services.AddSingleton<AnalyticsService>();
			services.AddSingleton<GithubProxy>();
			services.AddSingleton<HardwareAppIdService>();
			services.AddSingleton<SteamlessService>();
			services.AddSingleton<CloudRedirectService>();
			services.AddSingleton<UnlockerService>();
			services.AddSingleton<DefenderService>();
			services.AddSingleton<PluginInstallerService>();
			services.AddTransient<DropInstallViewModel>();
			services.AddSingleton<GabLuchiApiClient>();
			services.AddSingleton<ManifestDownloader>();
			services.AddSingleton<FixRepository>();
			services.AddSingleton<HubcapService>();
			services.AddSingleton<UpdateService>();
			services.AddSingleton<PluginAddService>();
			services.AddSingleton<CompanionService>();
			services.AddHostedService((IServiceProvider sp) => sp.GetRequiredService<CompanionService>());
			services.AddSingleton<HttpServerService>();
			services.AddHostedService((IServiceProvider sp) => sp.GetRequiredService<HttpServerService>());
			services.AddSingleton<CefInjectorService>();
			services.AddHostedService((IServiceProvider sp) => sp.GetRequiredService<CefInjectorService>());
			services.AddSingleton<DownloadViewModel>();
			services.AddSingleton<SettingsViewModel>();
			services.AddSingleton<ManageViewModel>();
			services.AddSingleton<HomeViewModel>();
			services.AddSingleton<ModeViewModel>();
			services.AddSingleton<FixesViewModel>();
			services.AddSingleton<PluginViewModel>();
			services.AddSingleton<OnboardingViewModel>();
			services.AddSingleton<MainViewModel>();
			services.AddSingleton<HomeView>();
			services.AddSingleton<DownloadView>();
			services.AddSingleton<ManageView>();
			services.AddSingleton<ModeView>();
			services.AddSingleton<FixesView>();
			services.AddSingleton<PluginView>();
			services.AddSingleton<SettingsView>();
			services.AddSingleton<MainWindow>();
		}).Build();
	}

	private async Task RunUpdateFlowAsync()
	{
		if (!_updateFlowGate.Wait(0))
		{
			return;
		}
		try
		{
			try
			{
				await Updates.CheckAndStageAsync();
			}
			catch
			{
			}
			if (Updates.HasStagedUpdate)
			{
				((DispatcherObject)this).Dispatcher.Invoke((Action)delegate
				{
					Updates.ApplyAndRestart(new string[2] { "--minimized", "--tray-locked" });
				});
				return;
			}
			try
			{
				PluginInstallerService installer = _host.Services.GetRequiredService<PluginInstallerService>();
				PluginStatus pluginStatus = await installer.GetStatusAsync(force: true);
				if (!pluginStatus.UpdateAvailable)
				{
					return;
				}
				if (!pluginStatus.DllMatches)
				{
					ToastService t = _host.Services.GetRequiredService<ToastService>();
					((DispatcherObject)this).Dispatcher.Invoke((Action)delegate
					{
						t.Show("GabLuchi", "Updating plugin — Steam will restart.");
					});
				}
				await installer.InstallAsync(null);
			}
			catch
			{
			}
		}
		finally
		{
			_updateFlowGate.Release();
		}
	}

	protected override async void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);
		Task.Run(delegate
		{
			try
			{
				string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "GabLuchi");
				if (Directory.Exists(path))
				{
					Directory.Delete(path, recursive: true);
				}
			}
			catch
			{
			}
		});
		await _host.StartAsync();
		MainViewModel main = _host.Services.GetRequiredService<MainViewModel>();
		SettingsViewModel settingsVm = _host.Services.GetRequiredService<SettingsViewModel>();
		settingsVm.RequestSignIn = () => main.Onboarding.SignInCommand.ExecuteAsync(null);
		settingsVm.RequestRestart = RelaunchApp;
		MainWindow window = _host.Services.GetRequiredService<MainWindow>();
		settingsVm.RequestShowWindow = delegate
		{
			((DispatcherObject)this).Dispatcher.Invoke((Action)window.RestoreFromTray);
		};
		if (Program.ShowWindowSignal != null)
		{
			ThreadPool.RegisterWaitForSingleObject(Program.ShowWindowSignal, delegate
			{
				((DispatcherObject)this).Dispatcher.Invoke((Action)delegate
				{
					string text = ProtocolService.TryReadPending();
					if (text == null || !ProtocolService.Parse(text).Silent)
					{
						window.RestoreFromTray();
					}
					if (text != null)
					{
						HandleProtocolUrl(text);
					}
				});
			}, null, -1, executeOnlyOnce: false);
		}
		if (Program.EnableTrayLockSignal != null)
		{
			ThreadPool.RegisterWaitForSingleObject(Program.EnableTrayLockSignal, delegate
			{
				Program.SessionTrayLock = true;
			}, null, -1, executeOnlyOnce: false);
		}
		if (Program.RecheckUpdatesSignal != null)
		{
			ThreadPool.RegisterWaitForSingleObject(Program.RecheckUpdatesSignal, delegate
			{
				RunUpdateFlowAsync();
			}, null, -1, executeOnlyOnce: false);
		}
		RunUpdateFlow = RunUpdateFlowAsync;
		ToastService toast = _host.Services.GetRequiredService<ToastService>();
		toast.Attach(window.RootSnackbar);
		settingsVm.RequestRestartPrompt = delegate
		{
			((DispatcherObject)this).Dispatcher.Invoke((Action)delegate
			{
				toast.ShowAction(Strings.Lang_Changed_Title, Strings.Lang_Changed_Body, Strings.Lang_Changed_Restart, delegate
				{
					settingsVm.RequestRestart?.Invoke();
				});
			});
		};
		DownloadViewModel download = _host.Services.GetRequiredService<DownloadViewModel>();
		ManageViewModel manage = _host.Services.GetRequiredService<ManageViewModel>();
		manage.NavigateToAdd = delegate(long appId)
		{
			((DispatcherObject)this).Dispatcher.Invoke((Action)delegate
			{
				window.NavigateToAdd();
				download.SeedSearch(appId);
			});
		};
		Action<long> navigateToGame = delegate(long appId)
		{
			((DispatcherObject)this).Dispatcher.Invoke((Action)delegate
			{
				window.NavigateToManage();
				manage.OpenDetailForAppIdAsync(appId);
			});
		};
		HomeViewModel home = _host.Services.GetRequiredService<HomeViewModel>();
		home.NavigateToGame = navigateToGame;
		download.NavigateToGame = navigateToGame;
		home.NavigateToPlugin = delegate
		{
			((DispatcherObject)this).Dispatcher.Invoke((Action)window.NavigateToPlugin);
		};
		home.NavigateToManage = delegate
		{
			((DispatcherObject)this).Dispatcher.Invoke((Action)window.NavigateToManage);
		};
		home.NavigateToSettings = delegate
		{
			((DispatcherObject)this).Dispatcher.Invoke((Action)window.NavigateToSettings);
		};
		home.NavigateToMode = delegate
		{
			((DispatcherObject)this).Dispatcher.Invoke((Action)window.NavigateToMode);
		};
		home.NavigateToAdd = delegate
		{
			((DispatcherObject)this).Dispatcher.Invoke((Action)window.NavigateToAdd);
		};
		home.RequestSignIn = () => main.Onboarding.SignInCommand.ExecuteAsync(null);
		main.Onboarding.RefreshHome = () => ((DispatcherObject)this).Dispatcher.Invoke<Task>((Func<Task>)(() => home.LoadAsync()));
		LuaInstaller requiredService = _host.Services.GetRequiredService<LuaInstaller>();
		SteamAppInfoCache appInfo = _host.Services.GetRequiredService<SteamAppInfoCache>();
		requiredService.Installed += delegate(long appId)
		{
			((DispatcherObject)this).Dispatcher.InvokeAsync<Task>((Func<Task>)async delegate
			{
				manage.LoadAsync();
				await home.RefreshLibraryAsync();
				if (await appInfo.EnsureFullDetailsAsync(appId))
				{
					await home.RefreshLibraryAsync();
				}
			});
		};
		string url = Program.StartupUrl ?? ProtocolService.TryReadPending();
		bool flag = (url != null && ProtocolService.Parse(url).Silent) || Program.StartMinimized;
		_exitAfterSilentInstall = flag && Program.StartupUrl != null && !settingsVm.MinimizeToTray;
		if (flag)
		{
			window.StartSilent();
		}
		else
		{
			window.Show();
			CacheService requiredService2 = _host.Services.GetRequiredService<CacheService>();
			if (Program.FirstRun)
			{
				await RunFirstRunAutoInstallAsync();
			}
			if (!requiredService2.OnboardingComplete)
			{
				UnlockerService requiredService3 = _host.Services.GetRequiredService<UnlockerService>();
				PluginInstallerService requiredService4 = _host.Services.GetRequiredService<PluginInstallerService>();
				bool flag2;
				switch (requiredService3.SelectedMode)
				{
				case UnlockerMode.OpenSteamTools:
				case UnlockerMode.OpenSteamToolsNightly:
					flag2 = true;
					break;
				default:
					flag2 = false;
					break;
				}
				if (flag2 && requiredService4.IsInstalledLocally())
				{
					requiredService2.OnboardingComplete = true;
				}
				else
				{
					main.Onboarding.IsOpen = true;
				}
			}
		}
		if (url != null)
		{
			HandleProtocolUrl(url);
		}
		if (Program.SessionTrayLock)
		{
			RunUpdateFlowAsync();
		}
		_host.Services.GetRequiredService<AnalyticsService>().TrackAppLaunchAsync();
		_host.Services.GetRequiredService<HardwareAppIdService>().EnsureFreshAsync();
	}

	private async Task RunFirstRunAutoInstallAsync()
	{
		try
		{
			SteamService steam = _host.Services.GetRequiredService<SteamService>();
			UnlockerService unlocker = _host.Services.GetRequiredService<UnlockerService>();
			ToastService toast = _host.Services.GetRequiredService<ToastService>();
			if (!steam.IsValid)
			{
				return;
			}
			ModeState state = await unlocker.GetStateAsync(UnlockerMode.OpenSteamTools);
			if (state.Status == ModeStatus.UpToDate)
			{
				return;
			}
			await Task.Run((Action)steam.StopSteam);
			ModeInstallResult result = await unlocker.InstallAsync(UnlockerMode.OpenSteamTools);
			if (result.Success)
			{
				bool restarted = await Task.Run((Func<bool>)steam.StartSteam);
				toast.Show(Strings.Mode_Toast_Updated, restarted ? string.Format(Strings.Mode_Toast_Updated_Restarting, UnlockerMode.OpenSteamTools) : string.Format(Strings.Mode_Toast_Updated_Start, UnlockerMode.OpenSteamTools));
			}
			else
			{
				await Task.Run((Func<bool>)steam.StartSteam);
				toast.Show(Strings.Mode_Toast_InstallFailed, result.Error ?? Strings.Mode_Toast_InstallFailed_Body, error: true);
			}
		}
		catch (Exception ex)
		{
			_host.Services.GetRequiredService<ToastService>().Show(Strings.Mode_Toast_InstallFailed, ex.Message, error: true);
		}
	}

	protected override async void OnExit(ExitEventArgs e)
	{
		if (Updates.HasStagedUpdate)
		{
			Updates.ApplyOnExit();
		}
		await _host.StopAsync();
		_host.Dispose();
		base.OnExit(e);
	}

	private void RelaunchApp()
	{
		try
		{
			string processPath = Environment.ProcessPath;
			if (processPath != null)
			{
				Process.Start(new ProcessStartInfo("cmd.exe", "/c timeout /t 2 /nobreak >nul & start \"\" \"" + processPath + "\"")
				{
					CreateNoWindow = true,
					UseShellExecute = false
				});
			}
		}
		catch
		{
		}
		finally
		{
			Shutdown();
		}
	}

	private void HandleProtocolUrl(string url)
	{
		var (text, num, flag) = ProtocolService.Parse(url);
		if (text == null || !num.HasValue)
		{
			return;
		}
		MainWindow window = _host.Services.GetRequiredService<MainWindow>();
		DownloadViewModel requiredService = _host.Services.GetRequiredService<DownloadViewModel>();
		ManageViewModel requiredService2 = _host.Services.GetRequiredService<ManageViewModel>();
		FixesViewModel requiredService3 = _host.Services.GetRequiredService<FixesViewModel>();
		switch (text)
		{
		case "game":
			window.NavigateToAdd();
			requiredService.SeedSearch(num.Value);
			break;
		case "install":
			if (flag)
			{
				requiredService.ProtocolInstall(num.Value, delegate(string msg, bool error)
				{
					((DispatcherObject)this).Dispatcher.Invoke((Action)delegate
					{
						window.ShowInstallNotification(msg, error);
						if (_exitAfterSilentInstall)
						{
							Task.Delay(6000).ContinueWith(delegate
							{
								((DispatcherObject)this).Dispatcher.Invoke((Action)base.Shutdown);
							});
						}
					});
				});
			}
			else
			{
				window.NavigateToAdd();
				requiredService.ProtocolInstall(num.Value);
			}
			break;
		case "manage":
			window.NavigateToManage();
			requiredService2.OpenDetailForAppIdAsync(num.Value);
			break;
		case "fix":
			window.NavigateToFixes();
			requiredService3.OpenForAppIdAsync(num.Value);
			break;
		}
	}
}
