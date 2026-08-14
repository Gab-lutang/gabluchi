using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;
using GabLuchi.Resources;
using GabLuchi.Services;

namespace GabLuchi.ViewModels;

public class DropInstallViewModel : ObservableObject
{
	private struct InstallTally
	{
		public int Luas = 0;

		public int Manifests = 0;

		public int Failed = 0;

		public List<string> Errors = new List<string>();

		public List<string> Skipped = new List<string>();

		public InstallTally()
		{
		}
	}

	private readonly LuaInstaller _installer;

	private readonly SteamAppListCache _appList;

	private readonly SteamAppInfoCache _appInfo;

	private readonly SteamDepotInfo _depotInfo;

	private IReadOnlyDictionary<long, ContentDepot> _depotsById = new Dictionary<long, ContentDepot>();

	[ObservableProperty]
	private bool _isDragOver;

	[ObservableProperty]
	[NotifyPropertyChangedFor("HasResult")]
	private string? _resultText;

	[ObservableProperty]
	private bool _resultFailed;

	[ObservableProperty]
	private bool _isConfirming;

	[ObservableProperty]
	private string _confirmTitle = "";

	[ObservableProperty]
	private string? _confirmNoChanges;

	[ObservableProperty]
	[NotifyPropertyChangedFor("HasAdded")]
	private List<DropDiffRow> _added = new List<DropDiffRow>();

	[ObservableProperty]
	[NotifyPropertyChangedFor("HasRemoved")]
	private List<DropDiffRow> _removed = new List<DropDiffRow>();

	private Queue<string>? _queue;

	private (string luaPath, long appId)? _pendingLua;

	private InstallTally _tally;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? confirmOverwriteCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private AsyncRelayCommand? cancelOverwriteCommand;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	private RelayCommand<DropDiffRow>? openSteamDbCommand;

	public bool HasResult => ResultText != null;

	public bool HasAdded => Added.Count > 0;

	public bool HasRemoved => Removed.Count > 0;

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsDragOver
	{
		get
		{
			return _isDragOver;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isDragOver, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsDragOver);
				_isDragOver = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsDragOver);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string? ResultText
	{
		get
		{
			return _resultText;
		}
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_resultText, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ResultText);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HasResult);
				_resultText = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ResultText);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HasResult);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool ResultFailed
	{
		get
		{
			return _resultFailed;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_resultFailed, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ResultFailed);
				_resultFailed = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ResultFailed);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsConfirming
	{
		get
		{
			return _isConfirming;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isConfirming, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsConfirming);
				_isConfirming = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsConfirming);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string ConfirmTitle
	{
		get
		{
			return _confirmTitle;
		}
		[MemberNotNull("_confirmTitle")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_confirmTitle, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ConfirmTitle);
				_confirmTitle = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ConfirmTitle);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string? ConfirmNoChanges
	{
		get
		{
			return _confirmNoChanges;
		}
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_confirmNoChanges, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.ConfirmNoChanges);
				_confirmNoChanges = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.ConfirmNoChanges);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public List<DropDiffRow> Added
	{
		get
		{
			return _added;
		}
		[MemberNotNull("_added")]
		set
		{
			if (!EqualityComparer<List<DropDiffRow>>.Default.Equals(_added, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Added);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HasAdded);
				_added = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Added);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HasAdded);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public List<DropDiffRow> Removed
	{
		get
		{
			return _removed;
		}
		[MemberNotNull("_removed")]
		set
		{
			if (!EqualityComparer<List<DropDiffRow>>.Default.Equals(_removed, value))
			{
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Removed);
				OnPropertyChanging(__KnownINotifyPropertyChangingArgs.HasRemoved);
				_removed = value;
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Removed);
				OnPropertyChanged(__KnownINotifyPropertyChangedArgs.HasRemoved);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand ConfirmOverwriteCommand => confirmOverwriteCommand ?? (confirmOverwriteCommand = new AsyncRelayCommand(ConfirmOverwrite));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IAsyncRelayCommand CancelOverwriteCommand => cancelOverwriteCommand ?? (cancelOverwriteCommand = new AsyncRelayCommand(CancelOverwrite));

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.RelayCommandGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public IRelayCommand<DropDiffRow> OpenSteamDbCommand => openSteamDbCommand ?? (openSteamDbCommand = new RelayCommand<DropDiffRow>(OpenSteamDb));

	public event Action? Installed;

	public DropInstallViewModel(LuaInstaller installer, SteamAppListCache appList, SteamAppInfoCache appInfo, SteamDepotInfo depotInfo)
	{
		_installer = installer;
		_appList = appList;
		_appInfo = appInfo;
		_depotInfo = depotInfo;
	}

	public async Task HandleDropAsync(IEnumerable<string> paths)
	{
		List<string> list = paths.Where(LuaInstaller.IsInstallable).ToList();
		if (list.Count == 0)
		{
			ResultFailed = true;
			ResultText = Strings.Drop_Nothing;
			return;
		}
		ResultText = null;
		_tally = new InstallTally();
		_queue = new Queue<string>(list);
		await ProcessQueueAsync();
	}

	private async Task ProcessQueueAsync()
	{
		while (true)
		{
			Queue<string> queue = _queue;
			if (queue == null || queue.Count <= 0)
			{
				break;
			}
			string path = _queue.Dequeue();
			string extension = Path.GetExtension(path);
			if (extension.Equals(".manifest", StringComparison.OrdinalIgnoreCase))
			{
				Apply(_installer.InstallManifestFile(path));
				continue;
			}
			long? appId;
			if (extension.Equals(".lua", StringComparison.OrdinalIgnoreCase))
			{
				appId = LuaInstaller.AppIdFromFileName(path);
				if (!appId.HasValue)
				{
					_tally.Skipped.Add(Path.GetFileName(path));
					continue;
				}
				if (await ConfirmIfOverwriteAsync(path, appId.Value, isZip: false))
				{
					return;
				}
				Apply(_installer.InstallLuaFile(path, appId.Value));
				continue;
			}
			appId = LuaInstaller.AppIdForZip(path);
			if (!appId.HasValue)
			{
				_tally.Skipped.Add(Path.GetFileName(path));
				continue;
			}
			if (await ConfirmIfOverwriteAsync(path, appId.Value, isZip: true))
			{
				return;
			}
			Apply(_installer.InstallZip(path, appId.Value));
		}
		FinishBatch();
	}

	private async Task<bool> ConfirmIfOverwriteAsync(string path, long appId, bool isZip)
	{
		string text = _installer.ReadInstalledLua(appId);
		if (text == null)
		{
			return false;
		}
		LuaContents oldLua = LuaFileParser.Parse(text, appId);
		LuaContents luaContents = (isZip ? ExtractLuaFromZip(path, appId) : LuaFileParser.Parse(path, appId));
		if ((object)luaContents == null)
		{
			return false;
		}
		LuaDiff diff = LuaFileParser.Diff(oldLua, luaContents);
		await _appList.EnsureLoadedAsync();
		_depotsById = await BuildDepotLookupAsync(appId);
		Added = diff.Added.Select(ToDiffRow).ToList();
		Removed = diff.Removed.Select(ToDiffRow).ToList();
		ConfirmTitle = string.Format(Strings.Drop_Confirm_Replace, NameFor(appId));
		ConfirmNoChanges = (diff.HasChanges ? null : Strings.Drop_Confirm_NoChanges);
		_pendingLua = (path, appId);
		IsConfirming = true;
		List<long> list = (from e in diff.Added.Concat(diff.Removed)
			where DisplayName(e) == null
			select e.Id).Distinct().ToList();
		if (list.Count > 0)
		{
			await Parallel.ForEachAsync(list, new ParallelOptions
			{
				MaxDegreeOfParallelism = 4
			}, async delegate(long id, CancellationToken _)
			{
				await _appInfo.ResolveAsync(id);
			});
			if (IsConfirming)
			{
				(string, long)? pendingLua = _pendingLua;
				if (pendingLua.HasValue && pendingLua.GetValueOrDefault().Item2 == appId)
				{
					Added = diff.Added.Select(ToDiffRow).ToList();
					Removed = diff.Removed.Select(ToDiffRow).ToList();
				}
			}
		}
		return true;
	}

	private async Task<IReadOnlyDictionary<long, ContentDepot>> BuildDepotLookupAsync(long appId)
	{
		Dictionary<long, ContentDepot> map = new Dictionary<long, ContentDepot>();
		AppDepotInfo appDepotInfo = await _depotInfo.GetAsync(appId);
		if ((object)appDepotInfo == null)
		{
			return map;
		}
		foreach (ContentDepot depot in appDepotInfo.Depots)
		{
			map[depot.Id] = depot;
			long? dlcAppId = depot.DlcAppId;
			if (dlcAppId.HasValue)
			{
				long valueOrDefault = dlcAppId.GetValueOrDefault();
				map[valueOrDefault] = depot;
			}
		}
		return map;
	}

	[RelayCommand]
	private async Task ConfirmOverwrite()
	{
		IsConfirming = false;
		(string, long)? pendingLua = _pendingLua;
		if (pendingLua.HasValue)
		{
			(string, long) valueOrDefault = pendingLua.GetValueOrDefault();
			string extension = Path.GetExtension(valueOrDefault.Item1);
			Apply(extension.Equals(".zip", StringComparison.OrdinalIgnoreCase) ? _installer.InstallZip(valueOrDefault.Item1, valueOrDefault.Item2) : _installer.InstallLuaFile(valueOrDefault.Item1, valueOrDefault.Item2));
		}
		_pendingLua = null;
		await ProcessQueueAsync();
	}

	[RelayCommand]
	private async Task CancelOverwrite()
	{
		IsConfirming = false;
		(string, long)? pendingLua = _pendingLua;
		if (pendingLua.HasValue)
		{
			(string, long) valueOrDefault = pendingLua.GetValueOrDefault();
			_tally.Skipped.Add(Path.GetFileName(valueOrDefault.Item1));
		}
		_pendingLua = null;
		await ProcessQueueAsync();
	}

	private void Apply(InstallResult r)
	{
		if (r.Error != null)
		{
			_tally.Errors.Add(r.Error);
		}
		_tally.Failed += r.Failed.Count;
		if (r.LuaInstalled)
		{
			_tally.Luas++;
		}
		_tally.Manifests += r.ManifestCount;
	}

	private void FinishBatch()
	{
		_queue = null;
		InstallTally tally = _tally;
		bool flag = (ResultFailed = tally.Failed > 0 || tally.Errors.Count > 0);
		List<string> list = new List<string>();
		if (tally.Luas > 0)
		{
			list.Add(string.Format(Strings.Drop_Count_Luas, tally.Luas));
		}
		if (tally.Manifests > 0)
		{
			list.Add(string.Format(Strings.Drop_Count_Manifests, tally.Manifests));
		}
		if (list.Count == 0 && !flag)
		{
			ResultText = ((tally.Skipped.Count > 0) ? Strings.Drop_NothingInstalled : null);
		}
		else
		{
			string text = ((list.Count > 0) ? string.Format(Strings.Drop_Result_Installed, string.Join(" + ", list)) : "");
			string text2 = ((tally.Failed > 0) ? string.Format(Strings.Drop_Result_Failed, tally.Failed) : "");
			string text3 = ((tally.Errors.Count > 0) ? (" " + tally.Errors[0]) : "");
			ResultText = (text + text2 + text3).Trim() + ((list.Count > 0) ? Strings.Drop_Result_RestartApply : "");
		}
		if (tally.Luas > 0 || tally.Manifests > 0)
		{
			this.Installed?.Invoke();
		}
	}

	private string? DisplayName(LuaEntry e)
	{
		long? num = _depotsById.GetValueOrDefault(e.Id)?.DlcAppId;
		if (num.HasValue)
		{
			long valueOrDefault = num.GetValueOrDefault();
			string text = _appList.GetName(valueOrDefault) ?? _appInfo.GetCached(valueOrDefault)?.Name;
			if (text != null)
			{
				return text;
			}
		}
		return _appList.GetName(e.Id) ?? _appInfo.GetCached(e.Id)?.Name ?? e.Comment;
	}

	private DropDiffRow ToDiffRow(LuaEntry e)
	{
		ContentDepot valueOrDefault = _depotsById.GetValueOrDefault(e.Id);
		bool flag = valueOrDefault?.IsDlc ?? false;
		bool flag2 = valueOrDefault?.IsShared ?? false;
		string? title = DisplayName(e) ?? (flag ? string.Format(Strings.Manage_DlcName, valueOrDefault.DlcAppId) : (flag2 ? Strings.Manage_SharedDepot : (e.HasKey ? Strings.Manage_Depot : string.Format(Strings.Manage_DlcName, e.Id))));
		List<string> list = new List<string> { e.Id.ToString() };
		if ((object)valueOrDefault != null && valueOrDefault.Size > 0)
		{
			list.Add(FormatSize(valueOrDefault.Size));
		}
		if (!string.IsNullOrWhiteSpace(valueOrDefault?.Os))
		{
			list.Add(PrettyOs(valueOrDefault.Os));
		}
		if (!string.IsNullOrWhiteSpace(valueOrDefault?.Language))
		{
			list.Add(valueOrDefault.Language);
		}
		long? num = valueOrDefault?.DlcAppId;
		object obj;
		if (num.HasValue)
		{
			long valueOrDefault2 = num.GetValueOrDefault();
			obj = $"https://steamdb.info/app/{valueOrDefault2}/";
		}
		else
		{
			obj = $"https://steamdb.info/depot/{e.Id}/";
		}
		string steamDbUrl = (string)obj;
		return new DropDiffRow(title, string.Join("  ·  ", list), flag, flag2, steamDbUrl);
	}

	[RelayCommand]
	private static void OpenSteamDb(DropDiffRow row)
	{
		SteamService.OpenUrl(row.SteamDbUrl);
	}

	private string NameFor(long appId)
	{
		return _appList.GetName(appId) ?? _appInfo.GetCached(appId)?.Name ?? appId.ToString();
	}

	private static string FormatSize(long bytes)
	{
		if (bytes <= 0)
		{
			return "";
		}
		double num = (double)bytes / 1024.0 / 1024.0 / 1024.0;
		if (!(num >= 1.0))
		{
			return $"{(double)bytes / 1024.0 / 1024.0:0.#} MB";
		}
		return $"{num:0.##} GB";
	}

	private static string PrettyOs(string os)
	{
		switch (os)
		{
		case "windows":
			return "Windows";
		case "macos":
		case "macosx":
			return "macOS";
		case "linux":
			return "Linux";
		default:
			return os;
		}
	}

	private static LuaContents? ExtractLuaFromZip(string zipPath, long appId)
	{
		try
		{
			using ZipArchive zipArchive = ZipFile.OpenRead(zipPath);
			ZipArchiveEntry zipArchiveEntry = zipArchive.Entries.FirstOrDefault((ZipArchiveEntry e) => e.Name.EndsWith(".lua", StringComparison.OrdinalIgnoreCase));
			if (zipArchiveEntry == null)
			{
				return null;
			}
			string text = Path.Combine(Path.GetTempPath(), $"gabluchi_drop_{appId}_{Guid.NewGuid():N}.lua");
			zipArchiveEntry.ExtractToFile(text, overwrite: true);
			try
			{
				return LuaFileParser.Parse(text, appId);
			}
			finally
			{
				try
				{
					File.Delete(text);
				}
				catch
				{
				}
			}
		}
		catch
		{
			return null;
		}
	}
}
