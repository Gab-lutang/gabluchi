# Online Multiplayer Fix — Implementation Plan

## Goal
Add direct download + auto-apply of online-fix.me multiplayer fixes to GabLuchi.

## Data Source: Perondepot Mirror
- **URL**: `http://api.perondepot.xyz/all/`
- **Format**: nginx autoindex of `.rar` files
- **Naming**: `[appid]_Game_Name.rar` (e.g., `[1245620]_Elden_Ring.rar`)
- **Password**: `online-fix.me` (universal)
- **Status**: Confirmed working, 3000+ games
- **No auth needed, no scraping, direct download**

## What We're Building
Direct download of online-fix.me archives from perondepot, auto-extract with known password, drop DLL files into game folder. One-click apply per game.

## What We're NOT Building
- License key / tier system (our tool is free)
- Denuvo token system (server-gated, not replicable)
- Our own API proxy (perondepot is the proxy)

---

## New Files

### 1. `GabLuchi.Models/OnlineFixEntry.cs`
```csharp
namespace GabLuchi.Models;

public record OnlineFixEntry(
    long AppId,
    string GameName,
    string FileName,
    long SizeBytes,
    string DownloadUrl
);
```

### 2. `GabLuchi.Models/OnlineFixApplyResult.cs`
```csharp
namespace GabLuchi.Models;

public record OnlineFixApplyResult(
    bool Success,
    int FilesInstalled,
    string? Error,
    string? GameDir
);
```

### 3. `GabLuchi.Services/OnlineFixService.cs`
Core service. Dependencies: `SteamLibraryService`, `ToastService`.

```csharp
public class OnlineFixService(
    SteamLibraryService library,
    ToastService toast)
{
    // Constants
    private const string PerondepotIndex = "http://api.perondepot.xyz/all/";
    private const string ArchivePassword = "online-fix.me";

    // Search perondepot index by game name
    public async Task<List<OnlineFixEntry>> SearchAsync(
        string query, CancellationToken ct);

    // Download RAR from perondepot
    public async Task<string> DownloadAsync(
        OnlineFixEntry entry,
        string tempDir,
        IProgress<double?>? progress,
        CancellationToken ct);

    // Extract with 7za.exe
    public string ExtractArchive(
        string archivePath,
        string outputDir);

    // Apply fix files to game directory
    public int ApplyToGame(
        string extractedDir,
        string gameDir);

    // Full pipeline
    public async Task<OnlineFixApplyResult> ApplyFixAsync(
        OnlineFixEntry entry,
        string gameDir,
        IProgress<double?>? progress,
        CancellationToken ct);
}
```

#### SearchAsync Implementation
- Fetch HTML from `http://api.perondepot.xyz/all/`
- Parse nginx autoindex: extract `<a href="...">` links matching `[appid]_*.rar`
- Filter by query (game name match) or AppID
- Return list of `OnlineFixEntry` with parsed metadata

#### DownloadAsync Implementation
- URL: `http://api.perondepot.xyz/all/{filename}`
- Save to `{tempDir}\{filename}`
- Report progress via `IProgress<double?>`
- Return path to downloaded archive

#### ExtractArchive Implementation
- Tool: `third_party/7za.exe` (already bundled in CrackFixService)
- Command: `7za.exe x -p"online-fix.me" -y "{archive}" -o"{outputDir}"`
- Parse stdout for progress
- Return output directory path

#### ApplyToGame Implementation
- Scan extracted directory for known fix files:
  - `steam_api64.dll` / `steam_api.dll`
  - `OnlineFix64.dll` / `OnlineFix.dll`
  - `winmm.dll`
  - `OnlineFix.ini`
  - `winhttp.dll`
  - `SteamOverlay64.dll`
  - `dnet.dll`
- Backup existing `steam_api64.dll` → `steam_api64.dll.bak`
- Copy fix files to game directory
- Write `OnlineFix.ini` with correct `RealAppId` if not present
- Return count of files installed

---

## Modified Files

### 4. `GabLuchi.ViewModels/MultiplayerFixViewModel.cs`
Add to existing ViewModel:
- `DownloadAndApplyCommand` (takes `OnlineFixEntry`)
- `IsDownloading` bool property
- `DownloadProgress` double property
- `DownloadStatusText` string property
- `ApplyResult` property for UI feedback
- Calls `OnlineFixService.ApplyFixAsync()` with progress reporting
- Shows toast on success/failure

### 5. `GabLuchi.Views/MultiplayerFixView.xaml`
Add per result:
- "Download & Apply" button (replaces or supplements "Open in Browser")
- Progress bar (visible during download)
- Status text ("Downloading...", "Extracting...", "Applying...", "Done!")
- "Applied" badge after successful install

### 6. `GabLuchi/App.cs`
Register `OnlineFixService` as singleton.

---

## Phase 1: Core Service (MVP)
- [ ] Create `OnlineFixEntry.cs`
- [ ] Create `OnlineFixApplyResult.cs`
- [ ] Create `OnlineFixService.cs` — SearchAsync, DownloadAsync, ExtractArchive, ApplyToGame
- [ ] Test perondepot search + download + extract with a known game
- [ ] Register in `App.cs`

## Phase 2: UI Integration
- [ ] Add `DownloadAndApplyCommand` to `MultiplayerFixViewModel`
- [ ] Add progress/status properties to ViewModel
- [ ] Update `MultiplayerFixView.xaml` — download button, progress bar, status
- [ ] Toast notifications for each stage

## Phase 3: Health Integration
- [ ] Add "Online Fix Detected" check to `GameHealthService`
- [ ] Detect `OnlineFix.ini` + `OnlineFix64.dll` in game directory
- [ ] Show status in game tiles
- [ ] Quick-fix button for online fix

---

## File Type Detection After Extraction
After extracting the RAR, we need to identify what's inside. The archive may contain:
- Just fix DLLs (small, ~5-50MB) — most common
- Full game + fix (large, 1-15GB) — some entries

We detect by checking extracted file sizes. If total > 500MB, it's likely a full game archive and we should warn the user.

## Extraction Tool
Reuses `third_party/7za.exe` already present from `CrackFixService`. No new binary needed.

```csharp
// Existing in CrackFixService:
public static bool ExtractArchive(string archivePath, string outputDir)
{
    var psi = new ProcessStartInfo("third_party/7za.exe",
        $"x -p\"cs.rin.ru\" -y \"{archivePath}\" -o\"{outputDir}\"");
    // ...
}
```

We use the same pattern with password `online-fix.me` instead.

---

## Security Considerations
- Validate downloaded file has RAR magic bytes before extraction
- Show user what files will be overwritten before applying
- Backup original `steam_api64.dll` as `.bak`
- Never auto-launch game after fix — let user decide

## Testing
- Search for "Among Us" → should find `[945360]_Among_Us.rar`
- Download → verify file size matches perondepot listing
- Extract → verify RAR password works with 7za.exe
- Apply → verify DLLs copied to game directory
- Test cancellation during download
- Test with game that has no online fix (graceful error)
