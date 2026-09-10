using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using GabLuchi.Models;
using GabLuchi.Resources;
using GabLuchi.Services;

namespace GabLuchi.ViewModels;

public class DownloadGamesViewModel : ObservableObject
{
	private readonly SteamRipService _steamRip;
	private readonly ToastService _toast;
	private List<SteamRipEntry> _allGames = new List<SteamRipEntry>();

	private string _searchText = "";
	private bool _isBusy;
	private string _statusMessage = "";
	private string _installDir = "";

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

	public string InstallDir
	{
		get => _installDir;
		set => SetProperty(ref _installDir, value);
	}

	public bool CanSearch => !IsBusy;

	public ObservableCollection<SteamRipEntry> Results { get; } = new ObservableCollection<SteamRipEntry>();

	public DownloadGamesViewModel(SteamRipService steamRip, ToastService toast)
	{
		_steamRip = steamRip;
		_toast = toast;
	}

	public async Task Search()
	{
		if (IsBusy) return;
		IsBusy = true;
		StatusMessage = Strings.DownloadGames_Loading;
		Results.Clear();
		try
		{
			_allGames = await _steamRip.FetchGamesAsync();
			List<SteamRipEntry> filtered = _steamRip.Search(SearchText.Trim(), _allGames);
			StatusMessage = string.Format(Strings.DownloadGames_Found, filtered.Count);
			foreach (SteamRipEntry entry in filtered)
				Results.Add(entry);
		}
		catch (Exception ex)
		{
			StatusMessage = ex.Message;
			_toast.Show(Strings.DownloadGames_Title, ex.Message, error: true);
		}
		finally { IsBusy = false; }
	}

	public void OpenMirror(string url)
	{
		SteamRipService.OpenInBrowser(url);
		StatusMessage = string.Format(Strings.DownloadGames_OpenedMirror, SteamRipService.GetHosterName(url));
	}

	public void ExtractDownloaded()
	{
		if (string.IsNullOrWhiteSpace(InstallDir))
		{
			_toast.Show(Strings.DownloadGames_Title, Strings.DownloadGames_NoInstallDir, error: true);
			return;
		}

		Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
		{
			Title = "Select downloaded archive",
			Filter = "Archives (*.rar;*.7z;*.zip)|*.rar;*.7z;*.zip|All files (*.*)|*.*"
		};

		if (dlg.ShowDialog() != true) return;

		string archivePath = dlg.FileName;
		StatusMessage = Strings.DownloadGames_Extracting;

		string extractDir = Path.Combine(Path.GetTempPath(), "GabLuchi", $"extract_{Guid.NewGuid().ToString("N")[..8]}");
		Directory.CreateDirectory(extractDir);

		try
		{
			bool extracted = ExtractArchive(archivePath, extractDir);
			if (!extracted)
			{
				StatusMessage = Strings.DownloadGames_ExtractFailed;
				_toast.Show(Strings.DownloadGames_Title, Strings.DownloadGames_ExtractFailed, error: true);
				return;
			}

			CopyExtractedFiles(extractDir, InstallDir);
			StatusMessage = Strings.DownloadGames_Done;
			_toast.Show(Strings.DownloadGames_Title, Strings.DownloadGames_Done);
		}
		catch (Exception ex)
		{
			StatusMessage = ex.Message;
			_toast.Show(Strings.DownloadGames_Title, ex.Message, error: true);
		}
		finally
		{
			try { Directory.Delete(extractDir, true); } catch { }
		}
	}

	private static bool ExtractArchive(string archivePath, string outputDir)
	{
		string sevenZipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7za.exe");
		if (!File.Exists(sevenZipPath)) return false;

		try
		{
			ProcessStartInfo psi = new ProcessStartInfo
			{
				FileName = sevenZipPath,
				Arguments = $"x \"{archivePath}\" -o\"{outputDir}\" -y",
				UseShellExecute = false,
				CreateNoWindow = true,
				WindowStyle = ProcessWindowStyle.Hidden
			};
			Process? proc = Process.Start(psi);
			proc?.WaitForExit(300000);
			return proc?.ExitCode == 0;
		}
		catch { return false; }
	}

	private static void CopyExtractedFiles(string sourceDir, string targetDir)
	{
		Directory.CreateDirectory(targetDir);
		foreach (string file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
		{
			string relative = Path.GetRelativePath(sourceDir, file);
			string dest = Path.Combine(targetDir, relative);
			string? destDir = Path.GetDirectoryName(dest);
			if (destDir != null) Directory.CreateDirectory(destDir);
			File.Copy(file, dest, overwrite: true);
		}
	}
}
