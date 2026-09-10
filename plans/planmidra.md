# SteaMidra (SFF) Feature Integration Plan

**Date:** 2026-09-07
**Source:** https://github.com/Midrags/SFF
**Target:** GabLuchi v1.0.14 → v2.0.0
**Status:** Planning

---

## Overview

Integrate 7 features from SteaMidra (SFF) + Steam-auto-crack into GabLuchi (Store Browser excluded — exposes free Hubcap backend). SFF is a Python/Qt-based Steam game management tool. Steam-auto-crack is a C#/.NET Steam auto-cracker. GabLuchi is a C#/WPF (.NET 8.0) tool using CommunityToolkit.Mvvm and Wpf.Ui. All features are re-implemented natively in C# following GabLuchi's existing architecture patterns.

**Distribution:** Both Velopack auto-update (existing customers) + new installer (new customers). DLC unlocker DLLs bundled in installer.

---

## Architecture Conventions (Must Follow)

### View Code-Behind Pattern

Every View follows this exact pattern (from `HomeView.cs`, `ModeView.cs`, etc.):

```csharp
using System.Windows.Controls;
using System.Windows.Markup;
using GabLuchi.ViewModels;

namespace GabLuchi.Views;

public partial class HomeView : UserControl, IComponentConnector
{
    private readonly HomeViewModel _viewModel;

    public HomeView(HomeViewModel viewModel)
    {
        InitializeComponent();
        base.DataContext = (_viewModel = viewModel);
        base.Loaded += async delegate
        {
            await _viewModel.LoadAsync();
        };
    }
}
```

Key rules:
- Class is `partial`, implements `IComponentConnector`
- Constructor takes ViewModel as parameter (DI resolves it)
- Sets `base.DataContext = viewModel`
- Optionally hooks `Loaded` to call `LoadAsync()`
- File naming: `ViewName.cs` (NOT `ViewName.xaml.cs`)

### DI Registration Pattern

All services, ViewModels, and Views are singletons (except `DropInstallViewModel` which is transient):

```csharp
services.AddSingleton<MyService>();
services.AddSingleton<MyViewModel>();
services.AddSingleton<MyView>();
```

### Namespace Convention

`<RootNamespace />` is empty in `.csproj`. Namespace = folder path:
- `GabLuchi.Services/MyService.cs` → `namespace GabLuchi.Services;`
- `GabLuchi.ViewModels/MyVm.cs` → `namespace GabLuchi.ViewModels;`
- `GabLuchi.Views/MyView.xaml` → `namespace GabLuchi.Views;`
- `GabLuchi.Models/MyModel.cs` → `namespace GabLuchi.Models;`

---

## Feature Comparison: SFF vs GabLuchi

| Feature | SFF | GabLuchi | Status |
|---|---|---|---|
| Lua/Manifest Handling | Yes | Yes | Existing |
| Steamless DRM Removal | Yes | Yes | Existing |
| System Tray | Yes | Yes | Existing |
| Auto-update | Yes | Yes | Existing |
| Settings Export/Import | Yes | Yes | Existing |
| Depot/DLC Inspection | Yes | Yes | Existing |
| Multiplayer Fix | Yes | **No** | **New** |
| Fixes & Bypasses (CrakFiles) | Yes | **No** | **New** |
| DLC Unlockers (CreamAPI/SmokeAPI) | Yes | **No** | **New** |
| Store Browser (Hubcap) | Yes (full) | Partial | **Excluded** (exposes free backend) |
| Cloud Saves | Yes | **No** | **New** |
| Older Version Downloads | Yes | **No** | **New** |
| Parallel Downloads | Yes | **No** | **New** |

---

## Phase 1: Multiplayer Fix + Fixes & Bypasses (3-5 days)

### 1A. Multiplayer Fix

**Source:** `sff/game/online_fix.py`

**New files:**
- `GabLuchi.Services/MultiplayerFixService.cs`
- `GabLuchi.ViewModels/MultiplayerFixViewModel.cs`
- `GabLuchi.Views/MultiplayerFixView.xaml`
- `GabLuchi.Views/MultiplayerFixView.cs`

**How it works (SFF):**
- Scrape `online-fix.me/index.php?do=search&subaction=search&story={game_name}`
- Fallback: Google/Bing `site:online-fix.me/games` search
- Score results with fuzzy matching (token coverage + `difflib.SequenceMatcher` ratio)
- Open best result in browser — no file downloads, no credentials

**Data model:**
```csharp
// GabLuchi.Models/MultiplayerFixResult.cs
namespace GabLuchi.Models;

public record MultiplayerFixResult(
    string Url,
    string Title,
    double Score
);
```

**Service implementation:**
```csharp
// GabLuchi.Services/MultiplayerFixService.cs
namespace GabLuchi.Services;

public class MultiplayerFixService
{
    // Search online-fix.me directly
    public async Task<List<MultiplayerFixResult>> SearchAsync(string gameName, CancellationToken ct);

    // Fallback: Google site: search
    private async Task<List<MultiplayerFixResult>> SearchGoogleAsync(string gameName, CancellationToken ct);

    // Fallback: Bing site: search
    private async Task<List<MultiplayerFixResult>> SearchBingAsync(string gameName, CancellationToken ct);

    // Score result relevance (token coverage + fuzzy ratio)
    private double ScoreResult(string gameName, string title, string href);

    // Open in browser
    public void OpenInBrowser(string url);
}
```

**View code-behind:**
```csharp
// GabLuchi.Views/MultiplayerFixView.cs
namespace GabLuchi.Views;

public partial class MultiplayerFixView : UserControl, IComponentConnector
{
    public MultiplayerFixView(MultiplayerFixViewModel viewModel)
    {
        InitializeComponent();
        base.DataContext = viewModel;
    }
}
```

**DI registration (App.cs):**
```csharp
services.AddSingleton<MultiplayerFixService>();
services.AddSingleton<MultiplayerFixViewModel>();
services.AddSingleton<MultiplayerFixView>();
```

**Navigation:** Add to `MainWindow.xaml` under Fixes section (see Navigation section).

---

### 1B. Fixes & Bypasses (CrakFiles)

**Source:** `sff/game/crack_fix.py`

**New files:**
- `GabLuchi.Services/CrakFilesService.cs`
- `GabLuchi.ViewModels/CrackFixViewModel.cs`
- `GabLuchi.Views/CrackFixView.xaml`
- `GabLuchi.Views/CrackFixView.cs`
- `GabLuchi.Models/CrackFixEntry.cs`

**How it works (SFF):**
- Fetch JSON from `https://raw.githubusercontent.com/KoriaPolis/CrakFiles/main/crackfiles.json`
- Search: exact match → substring contains
- Download from pixeldrain (extract file ID, download, extract with password `cs.rin.ru`)
- Uses `ZipFile.OpenRead` + `entry.ExtractToFile` for extraction

**Data model:**
```csharp
// GabLuchi.Models/CrackFixEntry.cs
namespace GabLuchi.Models;

public record CrackFixEntry(
    string BuildId,
    string Name,
    List<string> SourceCrack,
    List<string> OriginalDownload,
    List<CrackFixItem> Fixes
);

public record CrackFixItem(
    string Href,
    string Filename,
    string Size,
    List<string> Badges
);
```

**Service implementation:**
```csharp
// GabLuchi.Services/CrakFilesService.cs
namespace GabLuchi.Services;

public class CrakFilesService
{
    private const string CrackFilesUrl =
        "https://raw.githubusercontent.com/KoriaPolis/CrakFiles/main/crackfiles.json";

    // Fetch all fix entries
    public async Task<List<CrackFixEntry>> FetchFixesAsync(CancellationToken ct);

    // Search by game name
    public List<CrackFixEntry> SearchFixes(string query, List<CrackFixEntry> allFixes);

    // Download fix from pixeldrain
    public async Task<string> DownloadFixAsync(CrackFixItem fix, string tempDir, CancellationToken ct);

    // Extract to game folder
    public void ExtractToGameFolder(string zipPath, string gameDir);
}
```

**Pixeldrain download:**
- Extract file ID from URL: `https://buzzheavier.com/{id}` or `https://pixeldrain.com/u/{id}`
- Download: `https://pixeldrain.com/api/file/{id}`
- ZIP password: `cs.rin.ru`

**View code-behind:**
```csharp
// GabLuchi.Views/CrackFixView.cs
namespace GabLuchi.Views;

public partial class CrackFixView : UserControl, IComponentConnector
{
    public CrackFixView(CrackFixViewModel viewModel)
    {
        InitializeComponent();
        base.DataContext = viewModel;
    }
}
```

**DI registration (App.cs):**
```csharp
services.AddSingleton<CrakFilesService>();
services.AddSingleton<CrackFixViewModel>();
services.AddSingleton<CrackFixView>();
```

---

## Phase 2: DLC Unlockers (3-5 days)

**Source:** `sff/dlc_unlockers/`

**New files:**
- `GabLuchi.Services/DlcUnlockerBase.cs` (abstract)
- `GabLuchi.Services/SmokeApiUnlocker.cs`
- `GabLuchi.Services/CreamApiUnlocker.cs`
- `GabLuchi.Services/UplayR1Unlocker.cs`
- `GabLuchi.Services/UplayR2Unlocker.cs`
- `GabLuchi.Services/DlcUnlockerManager.cs`
- `GabLuchi.ViewModels/DlcUnlockerViewModel.cs`
- `GabLuchi.Views/DlcUnlockerView.xaml`
- `GabLuchi.Views/DlcUnlockerView.cs`
- `GabLuchi.Models/DlcUnlockerType.cs`
- `GabLuchi.Models/DlcUnlockerPlatform.cs`
- `GabLuchi.Models/DlcUnlockerInstallResult.cs`

**Models:**
```csharp
// GabLuchi.Models/DlcUnlockerType.cs
namespace GabLuchi.Models;

public enum DlcUnlockerType
{
    SmokeApi,
    CreamApi,
    UplayR1,
    UplayR2
}

// GabLuchi.Models/DlcUnlockerPlatform.cs
namespace GabLuchi.Models;

public enum DlcUnlockerPlatform
{
    Steam,
    Ubisoft
}

// GabLuchi.Models/DlcUnlockerInstallResult.cs
namespace GabLuchi.Models;

public record DlcUnlockerInstallResult(
    bool Success,
    string? Error,
    int DlcCount = 0
);
```

**Abstract base:**
```csharp
// GabLuchi.Services/DlcUnlockerBase.cs
namespace GabLuchi.Services;

public abstract class DlcUnlockerBase
{
    public abstract DlcUnlockerType Type { get; }
    public abstract DlcUnlockerPlatform Platform { get; }
    public abstract bool IsInstalled(string gameDir);
    public abstract Task<DlcUnlockerInstallResult> InstallAsync(
        string gameDir, List<int> dlcIds, int appId, CancellationToken ct);
    public abstract void Uninstall(string gameDir);
}
```

**Unlocker implementations:**

| Unlocker | GitHub Repo | DLL Names | Config Format |
|---|---|---|---|
| SmokeAPI | `acidicoala/SmokeAPI` | `smoke_api32.dll`, `smoke_api64.dll` | `SmokeAPI.config.json` (JSON) |
| CreamAPI | `acidicoala/CreamAPI` | `steam_api.dll` replacement | `cream_api.ini` (INI) |
| Uplay R1 | `acidicoala/UplayR1Unlocker` | `uplay_r1_loader.dll` | N/A |
| Uplay R2 | `acidicoala/UplayR2Unlocker` | `upc_r2_loader.dll` | N/A |

**SmokeAPI install flow:**
1. Validate game dir, write permissions, disk space (10MB)
2. Find all `steam_api.dll` / `steam_api64.dll` (recursive scan)
3. Backup original to `*_o.dll`
4. Copy SmokeAPI DLL as replacement
5. Write `SmokeAPI.config.json`:
```json
{
    "$version": 4,
    "outputs": {
        "<appid>": {
            "dlc": [dlc_id_1, dlc_id_2, ...],
            "force_load_appid": false
        }
    }
}
```

**CreamAPI install flow:**
1. Similar DLL replacement
2. Write `cream_api.ini`:
```ini
[steam]
appid=<appid>
dlc=<dlc_id_1>,<dlc_id_2>,...
```

**DlcUnlockerManager:**
```csharp
// GabLuchi.Services/DlcUnlockerManager.cs
namespace GabLuchi.Services;

public class DlcUnlockerManager
{
    // Detect platform from game directory
    public DlcUnlockerPlatform DetectPlatform(string gameDir);

    // Get active unlocker for a game
    public DlcUnlockerType? GetActiveUnlocker(int appId);

    // Set active unlocker
    public void SetActiveUnlocker(int appId, DlcUnlockerType type);

    // Install unlocker
    public async Task<DlcUnlockerInstallResult> InstallAsync(
        int appId, DlcUnlockerType type, List<int> dlcIds, CancellationToken ct);

    // Uninstall
    public void Uninstall(int appId);
}
```

**DLL bundling:** Include pre-downloaded DLLs in `third_party/dlc_unlockers/`:
```
third_party/
  dlc_unlockers/
    smoke_api64.dll
    cream_api.dll
    uplay_r1_loader.dll
    upc_r2_loader.dll
```

**View code-behind:**
```csharp
// GabLuchi.Views/DlcUnlockerView.cs
namespace GabLuchi.Views;

public partial class DlcUnlockerView : UserControl, IComponentConnector
{
    public DlcUnlockerView(DlcUnlockerViewModel viewModel)
    {
        InitializeComponent();
        base.DataContext = viewModel;
    }
}
```

**DI registration (App.cs):**
```csharp
services.AddSingleton<DlcUnlockerManager>();
services.AddSingleton<DlcUnlockerViewModel>();
services.AddSingleton<DlcUnlockerView>();
```

---

## Phase 3: Store Browser (5-7 days) — EXCLUDED FROM DEFAULT BUILD

> **Note:** Store Browser is excluded from the default build. Hubcap exposes a free backend (hubcapmanifest.com) that users could access directly, bypassing GabLuchi entirely. This would hurt retention. Code blocks retained for personal/internal use.

**Source:** `sff/network/store_browser.py`

**New files:**
- `GabLuchi.ViewModels/StoreBrowserViewModel.cs`
- `GabLuchi.Views/StoreBrowserView.xaml`
- `GabLuchi.Views/StoreBrowserView.cs`
- `GabLuchi.Models/StoreGameInfo.cs`
- `GabLuchi.Models/StoreLibraryPage.cs`

**Existing file to extend:** `GabLuchi.Services/HubcapService.cs`

No new service — extend existing `HubcapService` with new methods. This avoids DI duplication and keeps all Hubcap logic in one place.

**New methods to add to HubcapService.cs:**
```csharp
// GabLuchi.Services/HubcapService.cs additions

public async Task<StoreLibraryPage?> GetLibraryAsync(
    int limit, int offset, string? sortBy, string? search, CancellationToken ct);

public async Task<List<StoreGameInfo>> SearchAsync(string query, CancellationToken ct);

public async Task<List<StoreGameInfo>> GetAllGamesAsync(CancellationToken ct);

public async Task<List<int>> GenerateManifestDepotsAsync(int appId, CancellationToken ct);

public async Task<string?> DownloadLuaAsync(int appId, CancellationToken ct);
```

**Models:**
```csharp
// GabLuchi.Models/StoreGameInfo.cs
namespace GabLuchi.Models;

public record StoreGameInfo(
    int AppId,
    string Name,
    string? Status,
    long? Size,
    string? Platforms
);

// GabLuchi.Models/StoreLibraryPage.cs
namespace GabLuchi.Models;

public record StoreLibraryPage(
    List<StoreGameInfo> Games,
    int TotalCount,
    int Offset,
    int Limit
);
```

**Store browser API (base: `https://hubcapmanifest.com/api/v1`):**

| Endpoint | Method | Purpose |
|---|---|---|
| `/library` | GET | Paginated game list. Params: `limit`, `offset`, `sort_by`, `search` |
| `/search` | GET | Search games. Params: `q`, `limit`, `appid` (bool) |
| `/games` | GET | All games list |
| `/status/{appid}` | GET | Manifest status (existing) |
| `/manifest/{appid}` | GET | Download manifest (existing) |
| `/lua/{appid}` | GET | Download Lua file |
| `/generate/manifest/{appid}` | GET | Depot ID list |
| `/user/stats` | GET | API key validation (existing) |

**View code-behind:**
```csharp
// GabLuchi.Views/StoreBrowserView.cs
namespace GabLuchi.Views;

public partial class StoreBrowserView : UserControl, IComponentConnector
{
    public StoreBrowserView(StoreBrowserViewModel viewModel)
    {
        InitializeComponent();
        base.DataContext = viewModel;
    }
}
```

**DI registration (App.cs):**
```csharp
services.AddSingleton<StoreBrowserViewModel>();
services.AddSingleton<StoreBrowserView>();
```

---

## Phase 4: Cloud Saves (7-10 days)

**Source:** `sff/cloud/cloud_saves.py`, `sff/cloud/google_drive.py`, `sff/cloud/cloud_save_paths.py`

**New files:**
- `GabLuchi.Services/CloudSaveService.cs`
- `GabLuchi.Services/LudusaviManifestService.cs`
- `GabLuchi.Services/GoogleDriveService.cs`
- `GabLuchi.Services/RcloneService.cs`
- `GabLuchi.ViewModels/CloudSavesViewModel.cs`
- `GabLuchi.Views/CloudSavesView.xaml`
- `GabLuchi.Views/CloudSavesView.cs`
- `GabLuchi.Models/CloudBackupEntry.cs`
- `GabLuchi.Models/SavePath.cs`
- `GabLuchi.Models/CloudProvider.cs`

**Bundled data:**
- `sff/data/manifest.yaml` → `data/ludusavi_manifest.yaml` (~18MB, 22k games)

**New DLL dependencies (download manually, add as References):**

> **Note:** GabLuchi uses `<Reference>` with `<HintPath>` pointing to `$(GabLuchiRuntimeDir)` (`$(LOCALAPPDATA)\GabLuchi\current`), NOT `<PackageReference>`. Download DLLs from NuGet manually and place in the runtime directory.

```xml
<!-- Add to GabLuchi.csproj ItemGroup -->
<Reference Include="Google.Apis.Drive.v3">
  <HintPath>$(GabLuchiRuntimeDir)\Google.Apis.Drive.v3.dll</HintPath>
</Reference>
<Reference Include="Google.Apis">
  <HintPath>$(GabLuchiRuntimeDir)\Google.Apis.dll</HintPath>
</Reference>
<Reference Include="Google.Apis.Core">
  <HintPath>$(GabLuchiRuntimeDir)\Google.Apis.Core.dll</HintPath>
</Reference>
<Reference Include="YamlDotNet">
  <HintPath>$(GabLuchiRuntimeDir)\YamlDotNet.dll</HintPath>
</Reference>
```

**Models:**
```csharp
// GabLuchi.Models/SavePath.cs
namespace GabLuchi.Models;

public record SavePath(
    string Path,
    string Source,       // "steam_userdata", "ludusavi", "custom"
    bool Exists
);

// GabLuchi.Models/CloudBackupEntry.cs
namespace GabLuchi.Models;

public record CloudBackupEntry(
    string BackupDir,
    DateTime CreatedAt,
    int AppId,
    long TotalSizeBytes,
    int FileCount
);

// GabLuchi.Models/CloudProvider.cs
namespace GabLuchi.Models;

public enum CloudProvider
{
    Local,
    GoogleDrive,
    Rclone
}
```

### 4A. Ludusavi Manifest Service

```csharp
// GabLuchi.Services/LudusaviManifestService.cs
namespace GabLuchi.Services;

public class LudusaviManifestService
{
    // Lazy-load manifest.yaml (regex-based indexing, don't load entire YAML into memory)
    public List<SavePath> GetSavePaths(int appId, string platform = "windows");

    // Resolve placeholder tags
    // <base>, <root>, <home>, <winDocuments>, <winAppData>,
    // <winLocalAppData>, <xdgData>, <xdgConfig>, <storeUserId>, <osUserName>
    public string ResolvePath(string template, string gameInstallDir);
}
```

### 4B. Local Backup

```csharp
// GabLuchi.Services/CloudSaveService.cs
namespace GabLuchi.Services;

// Backup directory: %APPDATA%\GabLuchi\save_backups\{appid}\backup_YYYYMMDD_HHMMSS\
public class CloudSaveService
{
    // Scan Steam userdata
    public List<SavePath> ScanSteamUserdata(int appId);

    // Scan Ludusavi custom paths
    public List<SavePath> ScanLudusaviPaths(int appId);

    // Backup all saves for a game
    public async Task<string> BackupAsync(int appId, string? customDest, CancellationToken ct);

    // Restore from backup
    public async Task RestoreAsync(string backupDir, int appId, CancellationToken ct);

    // Safety backup before overwrite
    private async Task SafetyBackupAsync(int appId, CancellationToken ct);

    // List backups
    public List<CloudBackupEntry> ListBackups(int appId);
}
```

### 4C. Google Drive

```csharp
// GabLuchi.Services/GoogleDriveService.cs
namespace GabLuchi.Services;

public class GoogleDriveService
{
    // OAuth2 sign-in (similar to AuthService Discord flow)
    public async Task<bool> SignInAsync(CancellationToken ct);

    // Check if signed in
    public bool IsSignedIn { get; }

    // Upload backup
    public async Task UploadAsync(string localPath, string remotePath, CancellationToken ct);

    // Download backup
    public async Task DownloadAsync(string remotePath, string localPath, CancellationToken ct);

    // Smart sync (size comparison, skip unchanged)
    public async Task SmartSyncAsync(string localDir, string remoteDir, CancellationToken ct);

    // Token stored via DPAPI (same as AuthService)
}
```

### 4D. rclone

```csharp
// GabLuchi.Services/RcloneService.cs
namespace GabLuchi.Services;

public class RcloneService
{
    // Check if rclone is installed
    public bool IsInstalled { get; }

    // Copy files to remote
    public async Task CopyAsync(string source, string remote, IProgress<double>? progress, CancellationToken ct);

    // Scan remote for backups
    public async Task<List<string>> ScanRemoteAsync(string remote, CancellationToken ct);
}
```

**rclone command:**
```
rclone copy {source} {remote} --transfers 9 --checkers 18 --fast-list --create-empty-src-dirs
```

**View code-behind:**
```csharp
// GabLuchi.Views/CloudSavesView.cs
namespace GabLuchi.Views;

public partial class CloudSavesView : UserControl, IComponentConnector
{
    public CloudSavesView(CloudSavesViewModel viewModel)
    {
        InitializeComponent();
        base.DataContext = viewModel;
    }
}
```

**DI registration (App.cs):**
```csharp
services.AddSingleton<LudusaviManifestService>();
services.AddSingleton<CloudSaveService>();
services.AddSingleton<GoogleDriveService>();
services.AddSingleton<RcloneService>();
services.AddSingleton<CloudSavesViewModel>();
services.AddSingleton<CloudSavesView>();
```

---

## Phase 4: Older Versions (Under Development)

**Status:** Under Development — Implemented, not yet tested.

### 4A. DepotDownloaderMod Integration

**Source:** `sff/downloads/depot_downloader.py`

**New files:**
- `GabLuchi.Services/DepotDownloaderModService.cs`
- `GabLuchi.Models/DepotVersionEntry.cs`
- `GabLuchi.Models/DepotDownloadResult.cs`

**Bundled:** `third_party/DepotDownloaderMod/DepotDownloaderMod.dll` (extracted from `Release.rar` — framework-dependent build, requires .NET 9 runtime on customer machines)

**Models:**
```csharp
// GabLuchi.Models/DepotVersionEntry.cs
namespace GabLuchi.Models;

public record DepotVersionEntry(
    string ManifestId,
    string? Date,
    string? Size,
    int DepotId
);

// GabLuchi.Models/DepotDownloadResult.cs
namespace GabLuchi.Models;

public record DepotDownloadResult(
    bool Success,
    string? Error,
    int FilesDownloaded,
    string OutputDir
);
```

**Command:**
```
dotnet DepotDownloaderMod.dll -app {appid} -depot {depot_id} \
  -depotkeys {tempfile} -max-downloads 32 -validate \
  -dir {dest} -os windows \
  -manifest {manifest_id}
```

**Depot keys format:**
```
{depot_id};{key}
```

**Service implementation:**
```csharp
// GabLuchi.Services/DepotDownloaderModService.cs
namespace GabLuchi.Services;

public class DepotDownloaderModService
{
    // Check if .NET 9 is available (runs `dotnet --list-runtimes`)
    public async Task<bool> CheckDotNet9Async(CancellationToken ct);

    // Check if DepotDownloaderMod.dll exists in third_party/
    public bool IsToolPresent { get; }

    // Download specific depot version
    public async Task<DepotDownloadResult> DownloadDepotAsync(
        int appId, int depotId, string manifestId,
        string? depotKey, string destDir, IProgress<double?>? progress,
        CancellationToken ct);
}
```

**Progress parsing:** Regex `^\s*(\d{1,3}(?:\.\d+)?)%\s` from stdout via `OutputDataReceived` event.

**Error codes:**
| Exit Code | Meaning |
|---|---|
| -2146233082 | .NET unhandled exception (outdated DDMod) |
| -1073741819 | Missing DLL (reinstall .NET 9) |

**Implementation notes:**
- Uses `partial class` with `[GeneratedRegex]` for progress parsing
- 10-minute timeout via `CancellationTokenSource`
- `WorkingDirectory` set to tool directory so DLL dependencies resolve
- Depot keys written to temp file, cleaned up in `finally` block
- `SemaphoreSlim` gate for .NET 9 check (double-checked locking)

### 4B. SteamDB Browser (Simplified — No WebView2)

**New files:**
- `GabLuchi.ViewModels/OlderVersionViewModel.cs`
- `GabLuchi.Views/OlderVersionView.xaml`
- `GabLuchi.Views/OlderVersionView.cs`

**No WebView2 dependency.** SteamDB opens in external browser via `SteamService.OpenUrl`. User manually enters depot/manifest IDs into the form.

**ViewModel:**
```csharp
// GabLuchi.ViewModels/OlderVersionViewModel.cs
namespace GabLuchi.ViewModels;

public class OlderVersionViewModel : ObservableObject
{
    // Input properties: AppId, DepotId, ManifestId, DepotKey, DestDir
    // Status properties: IsDownloading, Progress, IsProgressIndeterminate, StatusMessage
    // Check properties: IsDotNet9Available, IsToolPresent, CanDownload

    // Commands:
    [RelayCommand] CheckDotNet()     // Verifies .NET 9 runtime
    [RelayCommand] OpenSteamDbForApp() // Opens steamdb.info/app/{id}/depots/
    [RelayCommand] BrowseDest()      // Folder picker dialog
    [RelayCommand] Download()        // Runs DepotDownloaderMod
    [RelayCommand] CancelDownload()  // Cancels in-progress download
}
```

**View layout:**
- App ID, Depot ID, Manifest ID, Depot Key (optional) text inputs
- Destination folder picker with Browse button
- "Open SteamDB" button → external browser
- ".NET 9 Status" indicator + "Check .NET 9" button
- "Download" + "Cancel" buttons
- Progress bar + status text
- Help section with usage instructions

**View code-behind:**
```csharp
// GabLuchi.Views/OlderVersionView.cs
namespace GabLuchi.Views;

public partial class OlderVersionView : UserControl, IComponentConnector
{
    public OlderVersionView(OlderVersionViewModel viewModel)
    {
        InitializeComponent();
        base.DataContext = viewModel;
    }
}
```

**DI registration (App.cs):**
```csharp
services.AddSingleton<DepotDownloaderModService>();
services.AddSingleton<OlderVersionViewModel>();
services.AddSingleton<OlderVersionView>();
```

---

## DI Registration Summary

All additions to `App.cs` `ConfigureServices` (append after existing registrations):

```csharp
// Phase 1
services.AddSingleton<MultiplayerFixService>();
services.AddSingleton<MultiplayerFixViewModel>();
services.AddSingleton<MultiplayerFixView>();
services.AddSingleton<CrakFilesService>();
services.AddSingleton<CrackFixViewModel>();
services.AddSingleton<CrackFixView>();

// Phase 2
services.AddSingleton<DlcUnlockerManager>();
services.AddSingleton<DlcUnlockerViewModel>();
services.AddSingleton<DlcUnlockerView>();

// ~~Phase 3~~ EXCLUDED — Store Browser (exposes free Hubcap backend)
// services.AddSingleton<StoreBrowserViewModel>();
// services.AddSingleton<StoreBrowserView>();

// Phase 3
services.AddSingleton<LudusaviManifestService>();
services.AddSingleton<CloudSaveService>();
services.AddSingleton<GoogleDriveService>();
services.AddSingleton<RcloneService>();
services.AddSingleton<CloudSavesViewModel>();
services.AddSingleton<CloudSavesView>();

// Phase 5 — IMPLEMENTED, NOT YET TESTED
services.AddSingleton<DepotDownloaderModService>();
services.AddSingleton<OlderVersionViewModel>();
services.AddSingleton<OlderVersionView>();

// Phase 6
services.AddSingleton<GoldbergService>();
services.AddSingleton<SteamApiCheckBypassService>();
services.AddSingleton<RestoreService>();

// Phase 7 — UNDER DEVELOPMENT
services.AddSingleton<SteamRipService>();
services.AddSingleton<DownloadGamesViewModel>();
services.AddSingleton<DownloadGamesView>();
```

---

## DLL Dependencies

GabLuchi uses `<Reference>` with `<HintPath>` pointing to `$(GabLuchiRuntimeDir)` (`$(LOCALAPPDATA)\GabLuchi\current`), NOT `<PackageReference>`. Download DLLs manually and place in the runtime directory:

| Package | Version | Source | Place In |
|---|---|---|---|
| Google.Apis.Drive.v3 | 1.68.0 | NuGet | `$(GabLuchiRuntimeDir)` |
| Google.Apis | latest | NuGet (dependency) | `$(GabLuchiRuntimeDir)` |
| Google.Apis.Core | latest | NuGet (dependency) | `$(GabLuchiRuntimeDir)` |
| YamlDotNet | 16.1.3 | NuGet | `$(GabLuchiRuntimeDir)` |

Add to `GabLuchi.csproj` `<ItemGroup>` (append after existing `<Reference>` entries):
```xml
<Reference Include="Google.Apis.Drive.v3">
  <HintPath>$(GabLuchiRuntimeDir)\Google.Apis.Drive.v3.dll</HintPath>
</Reference>
<Reference Include="Google.Apis">
  <HintPath>$(GabLuchiRuntimeDir)\Google.Apis.dll</HintPath>
</Reference>
<Reference Include="Google.Apis.Core">
  <HintPath>$(GabLuchiRuntimeDir)\Google.Apis.Core.dll</HintPath>
</Reference>
<Reference Include="YamlDotNet">
  <HintPath>$(GabLuchiRuntimeDir)\YamlDotNet.dll</HintPath>
</Reference>
```

---

## Navigation Items (MainWindow.xaml)

Add to `NavigationView.MenuItems` (grouped to avoid sidebar overload):

```xml
<!-- ~~Store EXCLUDED — exposes free Hubcap backend~~ -->
<!-- <ui:NavigationViewItem Content="Store" TargetPageType="{x:Type views:StoreBrowserView}">
  <ui:NavigationViewItem.Icon>
    <ui:SymbolIcon Symbol="ShoppingBag24" />
  </ui:NavigationViewItem.Icon>
</ui:NavigationViewItem> -->
<ui:NavigationViewItem Content="Cloud Saves" TargetPageType="{x:Type views:CloudSavesView}">
  <ui:NavigationViewItem.Icon>
    <ui:SymbolIcon Symbol="CloudFlow24" />
  </ui:NavigationViewItem.Icon>
</ui:NavigationViewItem>
<ui:NavigationViewItem Content="DLC Unlockers" TargetPageType="{x:Type views:DlcUnlockerView}">
  <ui:NavigationViewItem.Icon>
    <ui:SymbolIcon Symbol="Key24" />
  </ui:NavigationViewItem.Icon>
</ui:NavigationViewItem>
<ui:NavigationViewItem Content="Multiplayer" TargetPageType="{x:Type views:MultiplayerFixView}">
  <ui:NavigationViewItem.Icon>
    <ui:SymbolIcon Symbol="People24" />
  </ui:NavigationViewItem.Icon>
</ui:NavigationViewItem>
<ui:NavigationViewItem Content="Crack Fixes" TargetPageType="{x:Type views:CrackFixView}">
  <ui:NavigationViewItem.Icon>
    <ui:SymbolIcon Symbol="Wrench24" />
  </ui:NavigationViewItem.Icon>
</ui:NavigationViewItem>
<ui:NavigationViewItem Content="Older Versions" TargetPageType="{x:Type views:OlderVersionView}">
  <ui:NavigationViewItem.Icon>
    <ui:SymbolIcon Symbol="History24" />
  </ui:NavigationViewItem.Icon>
</ui:NavigationViewItem>
<ui:NavigationViewItem Content="Download Games" TargetPageType="{x:Type views:DownloadGamesView}">
  <ui:NavigationViewItem.Icon>
    <ui:SymbolIcon Symbol="ArrowDown24" />
  </ui:NavigationViewItem.Icon>
</ui:NavigationViewItem>
```

**Total sidebar items:** 7 existing + 1 new (Older Versions) + 1 new (Download Games) = 9. Other nav items pending Phase 1-3 implementation.

---

## Installer Updates

### Bundled third-party files
```
third_party/
  lib/
    Google.Apis.Drive.v3.dll
    Google.Apis.dll
    Google.Apis.Core.dll
    YamlDotNet.dll
  dlc_unlockers/
    smoke_api64.dll
    cream_api.dll
    uplay_r1_loader.dll
    upc_r2_loader.dll
  DepotDownloaderMod/
    DepotDownloaderMod.dll  ← extracted from Release.rar (framework-dependent, requires .NET 9)
  data/
    ludusavi_manifest.yaml
  goldberg/
    regular/
      x64/steam_api64.dll
      x86/steam_api.dll
    experimental/
      x64/steam_api64.dll
      x86/steam_api.dll
    tools/
      generate_interfaces_x64.exe
      generate_interfaces_x86.exe
  steam_api_bypass/
    SteamAPICheckBypass.dll
    SteamAPICheckBypass_x32.dll
```

### NSIS installer additions
```nsis
; DLL dependencies
File "third_party\lib\Google.Apis.Drive.v3.dll"
File "third_party\lib\Google.Apis.dll"
File "third_party\lib\Google.Apis.Core.dll"
File "third_party\lib\YamlDotNet.dll"

; DLC Unlocker DLLs
File "third_party\dlc_unlockers\smoke_api64.dll"
File "third_party\dlc_unlockers\cream_api.dll"
File "third_party\dlc_unlockers\uplay_r1_loader.dll"
File "third_party\dlc_unlockers\upc_r2_loader.dll"

; DepotDownloaderMod
File "third_party\DepotDownloaderMod\DepotDownloaderMod.dll"

; Ludusavi manifest
File "third_party\data\ludusavi_manifest.yaml"

; Goldberg Emulator
File /nonfatal "third_party\goldberg\regular\x64\steam_api64.dll"
File /nonfatal "third_party\goldberg\regular\x86\steam_api.dll"
File /nonfatal "third_party\goldberg\experimental\x64\steam_api64.dll"
File /nonfatal "third_party\goldberg\experimental\x86\steam_api.dll"

; SteamAPICheckBypass
File "third_party\steam_api_bypass\SteamAPICheckBypass.dll"
File "third_party\steam_api_bypass\SteamAPICheckBypass_x32.dll"
```

---

## File Summary

### New files to create (51 active, 5 excluded)

**Services (18):**
1. `GabLuchi.Services/MultiplayerFixService.cs`
2. `GabLuchi.Services/CrakFilesService.cs`
3. `GabLuchi.Services/DlcUnlockerBase.cs`
4. `GabLuchi.Services/SmokeApiUnlocker.cs`
5. `GabLuchi.Services/CreamApiUnlocker.cs`
6. `GabLuchi.Services/UplayR1Unlocker.cs`
7. `GabLuchi.Services/UplayR2Unlocker.cs`
8. `GabLuchi.Services/DlcUnlockerManager.cs`
9. `GabLuchi.Services/CloudSaveService.cs`
10. `GabLuchi.Services/LudusaviManifestService.cs`
11. `GabLuchi.Services/GoogleDriveService.cs`
12. `GabLuchi.Services/RcloneService.cs`
13. `GabLuchi.Services/DepotDownloaderModService.cs` **✅ IMPLEMENTED**
14. ~~`GabLuchi.Services/SteamDbBrowserService.cs`~~ **REMOVED** (external browser only)
15. `GabLuchi.Services/GoldbergService.cs` **✅ IMPLEMENTED**
16. `GabLuchi.Services/SteamApiCheckBypassService.cs` **✅ IMPLEMENTED**
17. `GabLuchi.Services/RestoreService.cs` **✅ IMPLEMENTED**
18. `GabLuchi.Services/SteamRipService.cs` **✅ IMPLEMENTED** (Phase 7 — Under Development)

**ViewModels (6 active, 1 excluded):**
15. `GabLuchi.ViewModels/MultiplayerFixViewModel.cs`
16. `GabLuchi.ViewModels/CrackFixViewModel.cs`
17. `GabLuchi.ViewModels/DlcUnlockerViewModel.cs`
18. ~~`GabLuchi.ViewModels/StoreBrowserViewModel.cs`~~ **EXCLUDED** (personal use only)
19. `GabLuchi.ViewModels/CloudSavesViewModel.cs`
20. `GabLuchi.ViewModels/OlderVersionViewModel.cs` **✅ IMPLEMENTED**
21. `GabLuchi.ViewModels/DownloadGamesViewModel.cs` **✅ IMPLEMENTED** (Phase 7 — Under Development)

**Views (12 active, 2 excluded):**
21. `GabLuchi.Views/MultiplayerFixView.xaml`
22. `GabLuchi.Views/MultiplayerFixView.cs`
23. `GabLuchi.Views/CrackFixView.xaml`
24. `GabLuchi.Views/CrackFixView.cs`
25. `GabLuchi.Views/DlcUnlockerView.xaml`
26. `GabLuchi.Views/DlcUnlockerView.cs`
27. ~~`GabLuchi.Views/StoreBrowserView.xaml`~~ **EXCLUDED** (personal use only)
28. ~~`GabLuchi.Views/StoreBrowserView.cs`~~ **EXCLUDED** (personal use only)
29. `GabLuchi.Views/CloudSavesView.xaml`
30. `GabLuchi.Views/CloudSavesView.cs`
31. `GabLuchi.Views/OlderVersionView.xaml` **✅ IMPLEMENTED**
32. `GabLuchi.Views/OlderVersionView.cs` **✅ IMPLEMENTED**
33. `GabLuchi.Views/DownloadGamesView.xaml` **✅ IMPLEMENTED** (Phase 7 — Under Development)
34. `GabLuchi.Views/DownloadGamesView.cs` **✅ IMPLEMENTED** (Phase 7 — Under Development)
35. `GabLuchi.Views/UrlToHosterConverter.cs` **✅ IMPLEMENTED** (Phase 7 — Under Development)

**Models (14 active, 2 excluded):**
33. `GabLuchi.Models/MultiplayerFixResult.cs`
34. `GabLuchi.Models/CrackFixEntry.cs`
35. `GabLuchi.Models/DlcUnlockerType.cs`
36. `GabLuchi.Models/DlcUnlockerPlatform.cs`
37. `GabLuchi.Models/DlcUnlockerInstallResult.cs`
38. ~~`GabLuchi.Models/StoreGameInfo.cs`~~ **EXCLUDED** (personal use only)
39. ~~`GabLuchi.Models/StoreLibraryPage.cs`~~ **EXCLUDED** (personal use only)
40. `GabLuchi.Models/CloudBackupEntry.cs`
41. `GabLuchi.Models/SavePath.cs`
42. `GabLuchi.Models/CloudProvider.cs`
43. `GabLuchi.Models/DepotVersionEntry.cs` **✅ IMPLEMENTED**
44. `GabLuchi.Models/DepotDownloadResult.cs` **✅ IMPLEMENTED**
45. `GabLuchi.Models/GoldbergApplyResult.cs` **✅ IMPLEMENTED**
46. `GabLuchi.Models/RestoreResult.cs` **✅ IMPLEMENTED**
47. `GabLuchi.Models/BypassMode.cs` **✅ IMPLEMENTED**
48. `GabLuchi.Models/SteamRipEntry.cs` **✅ IMPLEMENTED** (Phase 7 — Under Development)

### Existing files to modify (8 active, 1 excluded)
48. `GabLuchi/App.cs` — Add DI registrations **✅ MODIFIED**
49. `MainWindow.xaml` — Add navigation items **✅ MODIFIED**
50. ~~`GabLuchi.Services/HubcapService.cs` — Add store browser methods~~ **EXCLUDED** (personal use only)
51. `GabLuchi.csproj` — Add DLL references + Content entries **✅ MODIFIED**
52. `GabLuchi.Services/SteamlessService.cs` — Chain unpack → apply Goldberg → apply bypass **✅ MODIFIED**
53. `GabLuchi.ViewModels/ManageViewModel.cs` — Add RestoreCrackCommand **✅ MODIFIED**
54. `GabLuchi.Views/ManageView.xaml` — Add Restore button **✅ MODIFIED**
55. `GabLuchi.Resources/Strings.cs` — Add new string properties **✅ MODIFIED**
56. `GabLuchi.Resources/Strings.resx` — Add matching values **✅ MODIFIED**

---

## Phase 6: Goldberg Emulator + SteamAPICheckBypass + Restore (5-7 days)

**Source:** `SteamAutoCrack.Core/Utils/EMUApply.cs`, `SteamAutoCrack.Core/Utils/Restore.cs`, `SteamAutoCrack.Core/Utils/SteamStubUnpacker.cs` (SteamAPICheckBypass portion)

**What this adds:** Completes the DRM removal workflow. Currently GabLuchi unpacks SteamStub but the game still won't run without Steam API. This phase applies the Goldberg emulator (replaces steam_api.dll), adds anti-detection bypass, and provides a restore button to undo everything.

**Current workflow:** Remove DRM → Steamless unpacks EXE → done (game still fails without Steam)

**New workflow:** Remove DRM → Steamless unpacks EXE → Apply Goldberg (replace steam_api.dll) → Apply SteamAPICheckBypass (anti-detection) → done (game runs without Steam)

**New files (6):**
- `GabLuchi.Services/GoldbergService.cs`
- `GabLuchi.Services/SteamApiCheckBypassService.cs`
- `GabLuchi.Services/RestoreService.cs`
- `GabLuchi.Models/GoldbergApplyResult.cs`
- `GabLuchi.Models/RestoreResult.cs`
- `GabLuchi.Models/BypassMode.cs`

**Existing files to modify (4):**
- `GabLuchi.Services/SteamlessService.cs` — Chain unpack → apply Goldberg → apply bypass
- `GabLuchi.ViewModels/ManageViewModel.cs` — Add RestoreCrackCommand
- `GabLuchi.Views/ManageView.xaml` — Add Restore button
- `GabLuchi/App.cs` — Register new services

**Bundled third-party files (in installer):**
```
third_party/
  goldberg/
    regular/
      x64/steam_api64.dll
      x86/steam_api.dll
    experimental/
      x64/steam_api64.dll
      x86/steam_api.dll
  steam_api_bypass/
    SteamAPICheckBypass.dll        (64-bit)
    SteamAPICheckBypass_x32.dll    (32-bit)
```

**No new NuGet packages.** No SteamKit2, no IniFile, no SharpCompress. INI files written with string I/O. DLLs bundled in installer.

### 6A. GoldbergService

```csharp
// GabLuchi.Services/GoldbergService.cs
namespace GabLuchi.Services;

public class GoldbergService
{
    private static string GoldbergDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "goldberg");

    // Generate minimal steam_settings/ config
    // Files: steam_appid.txt, configs.user.ini, configs.main.ini
    public string GenerateConfig(int appId, string installDir);

    // Apply Goldberg emulator to game directory
    // 1. Find all steam_api.dll / steam_api64.dll recursively
    // 2. For each: backup original as .dll.bak, copy Goldberg DLL
    // 3. Copy steam_settings/ folder alongside each DLL
    public async Task<GoldbergApplyResult> ApplyAsync(int appId, CancellationToken ct);

    // Check if Goldberg is already applied
    public bool IsApplied(string installDir);

    // Detect 32-bit vs 64-bit from PE header
    private bool Is64Bit(string exePath);
}
```

**Config generation** — minimal files:
1. `steam_appid.txt` — just the app ID number
2. `configs.user.ini` — account_name=Goldberg, account_steamid=76561197960287930
3. `configs.main.ini` — offline=true, disable_networking=true

**Apply logic** — simplified from Steam-auto-crack's EMUApply:
- Find steam_api.dll/steam_api64.dll in install dir (recursive)
- Backup originals as .dll.bak
- Copy Goldberg DLLs (regular, not experimental)
- Copy steam_settings/ folder next to each DLL
- Auto-detect 32/64-bit from game EXE via PE header (byte at offset 0x3C + 4)

### 6B. SteamApiCheckBypassService

```csharp
// GabLuchi.Services/SteamApiCheckBypassService.cs
namespace GabLuchi.Services;

public class SteamApiCheckBypassService
{
    private static string BypassDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "steam_api_bypass");

    // Apply bypass to game directory
    // 1. Find game EXE (reuse SteamlessService pattern)
    // 2. Detect 32-bit vs 64-bit
    // 3. Copy appropriate bypass DLL as winmm.dll next to EXE
    // 4. Write SteamAPICheckBypass.json with file_redirect rules
    public void Apply(string installDir, BypassMode mode = BypassMode.All);

    // Check if bypass is already applied
    public bool IsApplied(string installDir);

    // Remove bypass DLLs + JSON
    public void Remove(string installDir);
}
```

**JSON config:**
```json
{
  "<game.exe>": { "mode": "file_redirect", "to": "<game.exe>.bak", "file_must_exist": true },
  "<relative>/steam_settings": { "mode": "file_hide" },
  "<relative>/steam_api64.dll": { "mode": "file_redirect", "to": "<relative>/steam_api64.dll.bak", "file_must_exist": true }
}
```

### 6C. RestoreService

```csharp
// GabLuchi.Services/RestoreService.cs
namespace GabLuchi.Services;

public class RestoreService
{
    // Restore game to pre-cracked state
    // 1. Delete bypass DLLs (winmm.dll, version.dll, winhttp.dll)
    // 2. Delete SteamAPICheckBypass.json
    // 3. For every .bak file: delete current, rename .bak back to original
    // 4. Delete all steam_settings/ directories
    // 5. Delete steam_interfaces.txt, local_save.txt
    public async Task<RestoreResult> RestoreAsync(int appId, CancellationToken ct);

    // Bulk restore
    public async Task<RestoreResult> RestoreSelectedAsync(IEnumerable<int> appIds, CancellationToken ct);
}
```

### 6D. Modify SteamlessService.cs

Extend `PatchGameAsync` to chain the full workflow:

```csharp
// After existing Steamless unpack logic:
if (unpackSuccess)
{
    await _goldberg.ApplyAsync(appId, ct);
    _bypass.Apply(installDir);
}
```

### 6E. ManageViewModel additions

```csharp
// Single game restore
private AsyncRelayCommand<LuaTileViewModel?>? restoreCrackCommand;
public IAsyncRelayCommand<LuaTileViewModel?> RestoreCrackCommand => ...;

private async Task RestoreCrack(LuaTileViewModel? tile)
{
    // Confirmation dialog
    // Call _restore.RestoreAsync(tile.AppId)
    // Show toast with result
}

// Bulk restore
private AsyncRelayCommand? restoreCrackSelectedCommand;
public IAsyncRelayCommand RestoreCrackSelectedCommand => ...;
```

### 6F. ManageView.xaml addition

Add "Restore" button after "Remove DRM" button (line 340):

```xml
<ui:Button Margin="0,8,0,0" HorizontalAlignment="Stretch"
           Content="{x:Static res:Strings.Manage_Action_RestoreCrack}"
           Command="{Binding DataContext.RestoreCrackCommand, ElementName=Root}"
           CommandParameter="{Binding}"
           Icon="{ui:SymbolIcon ArrowUndo24}"
           IsEnabled="{Binding DataContext.NotBusy, ElementName=Root}" />
```

### Potential issues and mitigations

| Issue | Mitigation |
|---|---|
| Games without steam_api.dll (DRM-free) | `ApplyAsync` returns early if no steam_api DLLs found |
| Games with multiple steam_api.dll in subdirs | Recursive scan handles this (same as Steam-auto-crack) |
| 32-bit vs 64-bit mismatch | PE header check from game's main EXE determines which DLL to use |
| User clicks Remove DRM twice | `IsApplied()` check skips if already applied |
| Restore when no backup exists | `RestoreResult` reports "nothing to restore" |
| Goldberg config missing interface versions | Games work without `steam_interfaces.txt` — skip for now |
| Winmm.dll conflicts with other DLL hijacking | Use `version.dll` as alternative (configurable via BypassMode) |

### Proofread checklist

- [x] No new NuGet packages needed
- [x] No SteamKit2 dependency (uses existing SteamDepotInfo)
- [x] No IniFile dependency (string I/O for INI files)
- [x] No SharpCompress dependency (DLLs bundled, not downloaded)
- [x] Existing SteamlessService pattern reused for EXE resolution
- [x] Existing ManageViewModel pattern reused for commands
- [x] Existing ManageView pattern reused for UI buttons
- [x] DI registration follows existing singleton pattern
- [x] Backup convention (.bak) matches SteamlessService
- [x] Restore logic handles both EXE backups (Steamless) and DLL backups (Goldberg)
- [x] No conflicts with existing `UnlockerMode` enum (new `BypassMode` enum)
- [x] Namespace convention matches folder structure
- [x] File naming matches existing patterns (ViewName.cs, not .xaml.cs)

---

## Phase 7: Download Games (Under Development)

**Status:** Under Development — Browser-assisted download + extraction

**What this adds:** Allows users to search for full pre-installed cracked games when DRM bypass fails (e.g., Denuvo). Opens download pages in browser, then extracts downloaded archives to install directory.

**Current limitation:** All game hosters (megadb, buzzheavier, gofile, 1fichier, pixeldrain, filecrypt, datanodes) are Cloudflare-protected. Automated downloads are not possible from a C# app. Feature works as browser-assisted workflow.

**New files (6):**
- `GabLuchi.Services/SteamRipService.cs` **✅ IMPLEMENTED** — Fetches game database, search, URL detection
- `GabLuchi.Models/SteamRipEntry.cs` **✅ IMPLEMENTED** — Data models for SteamRip JSON
- `GabLuchi.ViewModels/DownloadGamesViewModel.cs` **✅ IMPLEMENTED** — Search, open mirrors, extract
- `GabLuchi.Views/DownloadGamesView.xaml` **✅ IMPLEMENTED** — UI layout
- `GabLuchi.Views/DownloadGamesView.cs` **✅ IMPLEMENTED** — Code-behind
- `GabLuchi.Views/UrlToHosterConverter.cs` **✅ IMPLEMENTED** — URL → hoster name converter

**Data source:** SteamRip GitHub JSON (`steamrip_games.json`) — currently has stale links. Website (steamrip.com) has current links but is Cloudflare-protected.

**User workflow:**
1. Search for game by name
2. See results with mirror buttons (MegaDB, BuzzHeavier, GoFile, etc.)
3. Click mirror → opens in browser
4. Download file manually
5. Click "Extract Downloaded Archive" → pick file → extracts to install dir

**Known issues:**
- GitHub JSON links are mostly dead (files removed by hosters)
- Cannot scrape steamrip.com (Cloudflare-protected)
- Cannot automate downloads from any hoster (all Cloudflare-protected)
- Feature marked as "Under Development" until a viable data source is found

**Future improvements needed:**
- Find alternative game database with working links
- Consider SteamUnlocked (Google Drive/MEGA/MediaFire) integration
- Maintain curated list of working links

---

## Timeline

### Not Started
| Phase | Features | Dependencies |
|---|---|---|
| Phase 1 | Multiplayer Fix + Fixes & Bypasses | None |
| Phase 2 | DLC Unlockers | SmokeAPI/CreamAPI DLLs bundled |
| ~~Phase 3~~ | ~~Store Browser~~ | ~~None~~ **EXCLUDED** (exposes free Hubcap backend) |
| Phase 3 | Cloud Saves | Google.Apis.Drive.v3, YamlDotNet DLLs |

### Under Development
| Phase | Features | Dependencies |
|---|---|---|
| Phase 4 | Older Versions | DepotDownloaderMod.dll bundled, .NET 9 runtime required on customer machines |
| Phase 7 | Download Games | SteamRip JSON data (stale) |

### Implemented
| Phase | Features | Dependencies |
|---|---|---|
| Phase 5 | Goldberg + SteamAPICheckBypass + Restore | Goldberg + bypass DLLs bundled |

| **Total** | **8 features** | **3 implemented, 2 under development, 3 not started** | |

---

## Source Reference

SFF source files for each feature:
- **Multiplayer Fix:** `sff/game/online_fix.py`
- **CrakFiles:** `sff/game/crack_fix.py`, `sff/network/pixeldrain.py`
- **DLC Unlockers:** `sff/dlc_unlockers/` (base.py, smokeapi.py, creamapi.py, uplay_r1.py, uplay_r2.py, manager.py)
- **Store Browser:** `sff/network/store_browser.py`
- **Cloud Saves:** `sff/cloud/cloud_saves.py`, `sff/cloud/google_drive.py`, `sff/cloud/cloud_save_paths.py`
- **Older Versions:** `sff/downloads/depot_downloader.py`, `sff/store/older_version.py`
- **Parallel Downloads:** `sff/downloads/download_manager.py`, `sff/downloads/native_downloader.py`

Steam-auto-crack source files:
- **Goldberg Apply:** `SteamAutoCrack.Core/Utils/EMUApply.cs`
- **Goldberg Config:** `SteamAutoCrack.Core/Utils/EMUConfig.cs`
- **Goldberg Game Info:** `SteamAutoCrack.Core/Utils/EMUGameInfo.cs`
- **Restore:** `SteamAutoCrack.Core/Utils/Restore.cs`
- **SteamAPICheckBypass:** `SteamAutoCrack.Core/Utils/SteamStubUnpacker.cs` (ApplySteamAPICheckBypass method)
