# Multiplayer Fix Storage — GitHub Distribution Plan

## Overview

Distribute multiplayer fix files to GabLuchi users via a public GitHub repository. No server, no relay, no external services. Just GitHub raw files.

## Repository Structure

```
GitHub: Gab-lutang/gabluchi-fixes (public)
├── index.json                    ← fix catalog
├── fixes/
│   ├── gang_beasts_123456.rar
│   ├── shieldwall_1216320.rar
│   └── ...
```

## index.json Schema

```json
[
  {
    "appId": 123456,
    "gameName": "Gang Beasts",
    "password": "online-fix.me",
    "fileName": "gang_beasts_123456.rar",
    "dateAdded": "2026-09-14"
  },
  {
    "appId": 1216320,
    "gameName": "Shieldwall",
    "password": "online-fix.me",
    "fileName": "shieldwall_1216320.rar",
    "dateAdded": "2026-09-14"
  }
]
```

### Fields

| Field | Type | Required | Description |
|---|---|---|---|
| appId | int | Yes | Steam AppId of the game |
| gameName | string | Yes | Human-readable game name |
| password | string | Yes | Archive password (usually `online-fix.me`) |
| fileName | string | Yes | Filename in fixes/ folder |
| dateAdded | string | Yes | ISO date (YYYY-MM-DD) |

## Download URL Pattern

```
https://raw.githubusercontent.com/Gab-lutang/gabluchi-fixes/main/fixes/{fileName}
```

No API key. No auth. No rate limits for normal usage.

## GabLuchi Search Flow

```
User searches "Gang Beasts"
        ↓
┌──────────────────┬──────────────────────┐
│ PeronDepot       │ GitHub Fixes          │
│ (existing)        │ (new)                 │
│ search perondepot │ GET raw.githubusercontent.com│
│                    │ /.../index.json       │
└────────┬─────────┴──────────┬───────────┘
         │                    │
         └────────┬───────────┘
                  ↓
         Filter by query
                  ↓
         Merge results (dedupe by AppId)
                  ↓
         Show to user
                  ↓
         User clicks Apply
                  ↓
         Download .rar from GitHub
                  ↓
         Extract + apply (same as perondepot flow)
```

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                   GabLuchi App                       │
│                                                       │
│  ┌───────────────────────────────────────────────┐   │
│  │              MultiplayerFix Page                │   │
│  │                                                 │   │
│  │  Search: "Gang Beasts"                          │   │
│  │           ↓                                     │   │
│  │  ┌─────────────┐  ┌─────────────────────┐      │   │
│  │  │ PeronDepot   │  │ GitHub Fixes        │      │   │
│  │  │ (existing)   │  │ (new)               │      │   │
│  │  └──────┬──────┘  └──────────┬──────────┘      │   │
│  │         └─────────┬──────────┘                  │   │
│  │                   ↓                              │   │
│  │         Merged Results                           │   │
│  │         ┌─────────────────────┐                 │   │
│  │         │ Gang Beasts  [peron] │                 │   │
│  │         │ Gang Beasts  [github] │ (if different) │   │
│  │         └─────────────────────┘                 │   │
│  └───────────────────────────────────────────────┘   │
│                                                       │
│  ┌───────────────────────────────────────────────┐   │
│  │              Fix Library (Phase 2)              │   │
│  │              Local storage for user's own fixes │   │
│  └───────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────┘
```

## New Files

| File | Purpose |
|---|---|
| `GabLuchi.Models/GitHubFixEntry.cs` | Data model for GitHub fix entries |
| `GabLuchi.Services/GitHubFixService.cs` | Fetch index, download archives |

## Updated Files

| File | Change |
|---|---|
| `GabLuchi.ViewModels/MultiplayerFixViewModel.cs` | Merge perondepot + GitHub results |
| `GabLuchi.Views/MultiplayerFixView.xaml` | Add source badges ("perondepot" vs "gabluchi-fixes") |
| `GabLuchi/App.cs` | Register `GitHubFixService` in DI |
| `GabLuchi/Resources/Strings.resx` | Add `GitHubFix_*` string resources |
| `processofbuilding.md` | Document fix distribution workflow |

## Data Model

```csharp
namespace GabLuchi.Models;

public class GitHubFixEntry
{
    public int AppId { get; set; }
    public string GameName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string DateAdded { get; set; } = string.Empty;

    public string DownloadUrl =>
        $"https://raw.githubusercontent.com/Gab-lutang/gabluchi-fixes/main/fixes/{FileName}";

    public string Source => "gabluchi-fixes";
}
```

## GitHubFixService.cs

```csharp
namespace GabLuchi.Services;

public class GitHubFixService
{
    private const string IndexUrl =
        "https://raw.githubusercontent.com/Gab-lutang/gabluchi-fixes/main/index.json";

    private const string FixesBaseUrl =
        "https://raw.githubusercontent.com/Gab-lutang/gabluchi-fixes/main/fixes/";

    private readonly HttpClient _http;

    public GitHubFixService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<GitHubFixEntry>> FetchIndexAsync()
    {
        try
        {
            var json = await _http.GetStringAsync(IndexUrl);
            return JsonSerializer.Deserialize<List<GitHubFixEntry>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    public async Task<List<GitHubFixEntry>> SearchAsync(string query)
    {
        var all = await FetchIndexAsync();
        var q = query.Trim().ToLowerInvariant();

        return all.Where(f =>
            f.GameName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            f.AppId.ToString() == q
        ).ToList();
    }

    public async Task<bool> DownloadFixAsync(GitHubFixEntry entry, string destPath)
    {
        try
        {
            var bytes = await _http.GetByteArrayAsync(entry.DownloadUrl);
            await File.WriteAllBytesAsync(destPath, bytes);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
```

## Merge Logic

```csharp
// In MultiplayerFixViewModel.cs
private async Task SearchAllSources(string query)
{
    var peronResults = await _onlineFixService.SearchAsync(query);
    var githubResults = await _gitHubFixService.SearchAsync(query);

    // Mark sources
    peronResults.ForEach(r => r.Source = "perondepot");
    githubResults.ForEach(r => r.Source = "gabluchi-fixes");

    // Dedupe: prefer perondepot if same AppId exists
    var merged = peronResults
        .Concat(githubResults.Where(g =>
            !peronResults.Any(p => p.AppId == g.AppId)))
        .ToList();

    Results = merged;
}
```

## DI Registration

```csharp
// In App.cs
services.AddSingleton<GitHubFixService>();
```

## UI — Source Badges

Each search result shows a badge indicating source:

| Badge | Meaning |
|---|---|
| `perondepot` | From perondepot mirror (online) |
| `gabluchi-fixes` | From GabLuchi GitHub repo |
| `library` | From local fix library (Phase 2) |

## Build Order

| Step | What | Time |
|---|---|---|
| 1 | Create `Gab-lutang/gabluchi-fixes` repo + add test fixes | 5 min (manual) |
| 2 | `GitHubFixEntry.cs` — data model | 5 min |
| 3 | `GitHubFixService.cs` — fetch index + download | 20 min |
| 4 | Update `MultiplayerFixViewModel.cs` — merge logic | 15 min |
| 5 | Update `MultiplayerFixView.xaml` — source badges | 10 min |
| 6 | Update `App.cs` — DI registration | 5 min |
| 7 | Update `Strings.resx` — string resources | 5 min |
| 8 | Build + test | 15 min |
| **Total** | | **~1.5 hours** |

## Manual Steps (User)

1. Create public repo `Gab-lutang/gabluchi-fixes`
2. Create `index.json` with fix entries
3. Create `fixes/` folder
4. Upload fix archives to `fixes/`
5. Push to GitHub

## How to Add a New Fix

1. Download fix from online-fix.me (or wherever)
2. Rename to `{game_name}_{appId}.rar` (e.g., `gang_beasts_123456.rar`)
3. Upload to `fixes/` folder in the repo
4. Add entry to `index.json`:
   ```json
   {
     "appId": 123456,
     "gameName": "Gang Beasts",
     "password": "online-fix.me",
     "fileName": "gang_beasts_123456.rar",
     "dateAdded": "2026-09-14"
   }
   ```
5. Commit + push
6. All GabLuchi users automatically see the new fix

## Future Additions

### Phase 2: Local Fix Library
- Users add their own fixes locally
- Stored in `%LocalAppData%\GabLuchi\fixes\`
- Search merges: perondepot + GitHub + local

### Phase 3: Export/Import
- Export local library as zip
- Share with other players
- Import via file picker

### Phase 4: Auto-Download
- GabLuchi fetches fix index on first launch
- Caches locally for offline use
- Updates periodically

## GitHub Limits

| Limit | Value | Impact |
|---|---|---|
| File size | 50 MB per file | Most fix archives are 5-20 MB |
| Repo size | 1 GB recommended | ~500 fixes at 2 MB average |
| API rate | 60 req/hr (unauthenticated) | Raw files don't count against API |
| Raw file access | Unlimited | No rate limit for raw.githubusercontent.com |

## Troubleshooting

| Issue | Solution |
|---|---|
| Fix not showing | Check index.json syntax, ensure fileName matches actual file |
| Download fails | Check file exists in fixes/ folder, verify URL |
| Slow search | Index.json might be large, consider pagination |
| Merge conflicts | Ensure perondepot and GitHub don't duplicate same AppId |
