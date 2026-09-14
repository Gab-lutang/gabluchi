using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GabLuchi.Models;
using GabLuchi.Resources;
using GabLuchi.Services;

namespace GabLuchi.ViewModels;

public class MultiplayerFixViewModel : ObservableObject
{
	private readonly MultiplayerFixService _service;
	private readonly OnlineFixService _onlineFix;
	private readonly SteamLibraryService _library;
	private readonly ToastService _toast;
	private readonly ConnectRelayService _relay;

	private string _searchText = "";
	private bool _isBusy;
	private string _statusMessage = "";
	private bool _isDownloading;
	private double _downloadProgress;
	private string _downloadStatus = "";
	private string _lobbyCode = "";
	private string _joinCodeText = "";
	private string _lobbyStatus = "";
	private bool _isHosting;
	private bool _isJoining;
	private string _joinedGameName = "";
	private long _joinedAppId;
	private string _joinedIp = "";
	private int _joinedPort;
	private string _hostGameName = "";
	private string _hostPort = "";

	public string SearchText
	{
		get => _searchText;
		set { if (SetProperty(ref _searchText, value)) OnPropertyChanged(nameof(CanSearch)); }
	}

	public bool IsBusy
	{
		get => _isBusy;
		set { if (SetProperty(ref _isBusy, value)) OnPropertyChanged(nameof(CanSearch)); }
	}

	public string StatusMessage
	{
		get => _statusMessage;
		set => SetProperty(ref _statusMessage, value);
	}

	public bool IsDownloading
	{
		get => _isDownloading;
		set => SetProperty(ref _isDownloading, value);
	}

	public double DownloadProgress
	{
		get => _downloadProgress;
		set => SetProperty(ref _downloadProgress, value);
	}

	public string DownloadStatus
	{
		get => _downloadStatus;
		set => SetProperty(ref _downloadStatus, value);
	}

	public string LobbyCode
	{
		get => _lobbyCode;
		set { if (SetProperty(ref _lobbyCode, value)) OnPropertyChanged(nameof(HasLobbyCode)); }
	}

	public string JoinCodeText
	{
		get => _joinCodeText;
		set { if (SetProperty(ref _joinCodeText, value)) OnPropertyChanged(nameof(CanJoin)); }
	}

	public string LobbyStatus
	{
		get => _lobbyStatus;
		set => SetProperty(ref _lobbyStatus, value);
	}

	public bool IsHosting
	{
		get => _isHosting;
		set { if (SetProperty(ref _isHosting, value)) OnPropertyChanged(nameof(CanShare)); }
	}

	public bool IsJoining
	{
		get => _isJoining;
		set => SetProperty(ref _isJoining, value);
	}

	public string JoinedGameName
	{
		get => _joinedGameName;
		set { if (SetProperty(ref _joinedGameName, value)) OnPropertyChanged(nameof(HasJoinedGame)); }
	}

	public long JoinedAppId
	{
		get => _joinedAppId;
		set => SetProperty(ref _joinedAppId, value);
	}

	public string JoinedIp
	{
		get => _joinedIp;
		set => SetProperty(ref _joinedIp, value);
	}

	public int JoinedPort
	{
		get => _joinedPort;
		set => SetProperty(ref _joinedPort, value);
	}

	public string HostGameName
	{
		get => _hostGameName;
		set { if (SetProperty(ref _hostGameName, value)) OnPropertyChanged(nameof(CanShare)); }
	}

	public string HostPort
	{
		get => _hostPort;
		set => SetProperty(ref _hostPort, value);
	}

	public bool CanSearch => !IsBusy && !string.IsNullOrWhiteSpace(SearchText);
	public bool CanShare => !IsHosting && !IsJoining && !string.IsNullOrWhiteSpace(HostGameName);
	public bool CanJoin => !IsHosting && !IsJoining && !string.IsNullOrWhiteSpace(JoinCodeText);
	public bool HasLobbyCode => !string.IsNullOrEmpty(LobbyCode);
	public bool HasJoinedGame => !string.IsNullOrEmpty(JoinedGameName);

	public ObservableCollection<OnlineFixEntry> Results { get; } = new ObservableCollection<OnlineFixEntry>();

	public ICommand SearchCmd => searchCommand ?? (searchCommand = new AsyncRelayCommand(Search));
	private AsyncRelayCommand? searchCommand;

	public ICommand DownloadAndApplyCmd => downloadAndApplyCommand ?? (downloadAndApplyCommand = new AsyncRelayCommand<OnlineFixEntry>(DownloadAndApply));
	private AsyncRelayCommand<OnlineFixEntry>? downloadAndApplyCommand;

	public ICommand SearchOnSiteCmd => searchOnSiteCommand ?? (searchOnSiteCommand = new RelayCommand(SearchOnSite));
	private RelayCommand? searchOnSiteCommand;

	public ICommand ShareLobbyCmd => shareLobbyCommand ?? (shareLobbyCommand = new AsyncRelayCommand(ShareLobby));
	private AsyncRelayCommand? shareLobbyCommand;

	public ICommand JoinLobbyCmd => joinLobbyCommand ?? (joinLobbyCommand = new AsyncRelayCommand(JoinLobby));
	private AsyncRelayCommand? joinLobbyCommand;

	public ICommand CopyCodeCmd => copyCodeCommand ?? (copyCodeCommand = new RelayCommand(CopyCode));
	private RelayCommand? copyCodeCommand;

	public MultiplayerFixViewModel(MultiplayerFixService service, OnlineFixService onlineFix, SteamLibraryService library, ToastService toast, ConnectRelayService relay)
	{
		_service = service;
		_onlineFix = onlineFix;
		_library = library;
		_toast = toast;
		_relay = relay;
	}

	private async Task Search()
	{
		if (IsBusy || string.IsNullOrWhiteSpace(SearchText))
		{
			return;
		}
		IsBusy = true;
		StatusMessage = "Searching perondepot...";
		Results.Clear();
		try
		{
			List<OnlineFixEntry> results = await _onlineFix.SearchAsync(SearchText.Trim());
			if (results.Count == 0)
			{
				StatusMessage = Strings.MultiplayerFix_NoResults;
			}
			else
			{
				StatusMessage = string.Format(Strings.MultiplayerFix_Found, results.Count);
				foreach (OnlineFixEntry entry in results)
				{
					Results.Add(entry);
				}
			}
		}
		catch (Exception ex)
		{
			StatusMessage = "Error: " + ex.Message;
			_toast.Show(Strings.MultiplayerFix_Title, ex.Message, error: true);
		}
		finally
		{
			IsBusy = false;
		}
	}

	private async Task DownloadAndApply(OnlineFixEntry? entry)
	{
		if (entry == null || IsDownloading)
		{
			return;
		}
		string? gameDir = _library.GetInstallDir(entry.AppId);
		if (string.IsNullOrEmpty(gameDir) || !Directory.Exists(gameDir))
		{
			List<string> roots = _library.GetLibraryRootsList();
			string rootList = roots.Count > 0
				? string.Join("\n", roots)
				: "(no libraries found)";
			string msg = string.Format(
				"AppId {0} not found in {1} Steam libraries:\n{2}",
				entry.AppId, roots.Count, rootList);
			_toast.Show(Strings.MultiplayerFix_Title, msg, error: true);
			return;
		}
		IsDownloading = true;
		DownloadProgress = 0;
		DownloadStatus = "Downloading...";
		try
		{
			IProgress<double?> progress = new Progress<double?>(p =>
			{
				if (p.HasValue)
				{
					DownloadProgress = p.Value * 100;
				}
				else
				{
					DownloadProgress = -1;
				}
			});
			OnlineFixApplyResult result = await _onlineFix.ApplyFixAsync(entry, gameDir, progress);
			if (result.Success)
			{
				DownloadStatus = "Done! " + result.FilesInstalled + " files installed to " + gameDir;
				DownloadProgress = 100;
				_toast.Show(Strings.MultiplayerFix_Title, "Online fix applied! " + result.FilesInstalled + " files installed.");
			}
			else
			{
				DownloadStatus = "Failed: " + result.Error;
				_toast.Show(Strings.MultiplayerFix_Title, result.Error ?? "Unknown error", error: true);
			}
		}
		catch (Exception ex)
		{
			DownloadStatus = "Failed: " + ex.Message;
			_toast.Show(Strings.MultiplayerFix_Title, ex.Message, error: true);
		}
		finally
		{
			IsDownloading = false;
		}
	}

	private void SearchOnSite()
	{
		string query = string.IsNullOrWhiteSpace(SearchText) ? "" : Uri.EscapeDataString(SearchText.Trim());
		MultiplayerFixService.OpenInBrowser("https://online-fix.me/index.php?do=search&subaction=search&story=" + query);
	}

	private async Task ShareLobby()
	{
		if (IsHosting || IsJoining)
		{
			return;
		}
		IsHosting = true;
		LobbyStatus = "Detecting your IP...";
		LobbyCode = "";
		try
		{
			int port = 0;
			int.TryParse(HostPort.Trim(), out port);
			string code = await _relay.ShareLobbyAsync(HostGameName.Trim(), 0, port);
			LobbyCode = code;
			LobbyStatus = "Share this code with your friend";
			Clipboard.SetText(code);
			_toast.Show(Strings.MultiplayerFix_Title, "Lobby code copied to clipboard: " + code);
		}
		catch (Exception ex)
		{
			LobbyStatus = "Failed: " + ex.Message;
			_toast.Show(Strings.MultiplayerFix_Title, ex.Message, error: true);
		}
		finally
		{
			IsHosting = false;
		}
	}

	private async Task JoinLobby()
	{
		if (IsHosting || IsJoining || string.IsNullOrWhiteSpace(JoinCodeText))
		{
			return;
		}
		IsJoining = true;
		LobbyStatus = "Connecting...";
		JoinedGameName = "";
		JoinedAppId = 0;
		JoinedIp = "";
		JoinedPort = 0;
		try
		{
			LobbyInfo? lobby = await _relay.JoinLobbyAsync(JoinCodeText.Trim());
			if (lobby == null)
			{
				LobbyStatus = "Invalid or expired code";
				_toast.Show(Strings.MultiplayerFix_Title, "Invalid or expired code", error: true);
			}
			else
			{
				JoinedGameName = lobby.GameName;
				JoinedAppId = lobby.AppId;
				JoinedIp = lobby.Ip;
				JoinedPort = lobby.Port;
				string connectStr = lobby.Ip + ":" + lobby.Port;
				Clipboard.SetText(connectStr);
				LobbyStatus = lobby.GameName + " — IP copied to clipboard";
				_toast.Show(Strings.MultiplayerFix_Title, "Connection info for " + lobby.GameName + " copied to clipboard");
			}
		}
		catch (Exception ex)
		{
			LobbyStatus = "Failed: " + ex.Message;
			_toast.Show(Strings.MultiplayerFix_Title, ex.Message, error: true);
		}
		finally
		{
			IsJoining = false;
		}
	}

	private void CopyCode()
	{
		if (!string.IsNullOrEmpty(LobbyCode))
		{
			Clipboard.SetText(LobbyCode);
			_toast.Show(Strings.MultiplayerFix_Title, "Code copied: " + LobbyCode);
		}
	}
}
