# Multiplayer Fix Storage — Distribution Plan

## Overview

GabLuchi distributes multiplayer fixes using a **GitHub repository as storage**. No servers, no APIs, no external services. Just a public GitHub repo hosting fix archives and an index file.

**Primary source:** PeronDepot mirror (existing, automatic)
**Secondary source:** GabLuchi Fixes GitHub repo (curated, manual)
**Future:** Local library for user's own fixes

---

## Architecture

```
User searches "Gang Beasts"
        ↓
┌──────────────────┬──────────────────────┐
│ PeronDepot       │ GitHub Fixes          │
│ (existing)        │ (new)                 │
│ Auto-search       │ Fetch index.json      │
│ Online mirror     │ Download from GitHub   │
└────────┬─────────┴──────────┬───────────┘
         │                    │
         └────────┬───────────┘
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

---

## GitHub Repository

**Repo:** `Gab-lutang/gabluchi-fixes` (public)

### Structure

```
gabluchi-fixes/
├── index.json                    ← fix catalog
├── fixes/
│   ├── gang_beasts_123456.rar
│   ├── shieldwall_1216320.rar
│   └── ...
└── README.md                     ← (optional) list of available fixes
```

### URLs

| Resource | URL |
|---|---|
| Index | `https://raw.githubusercontent.com/Gab-lutang/gabluchi-fixes/main/index.json` |
| Fix archive | `https://raw.githubusercontent.com/Gab-lutang/gabluchi-fixes/main/fixes/{fileName}` |

No API key. No auth. Just raw URLs.

---

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
| `appId` | int | Yes | Steam AppId of the game |
| `gameName` | string | Yes | Display name (e.g., "Gang Beasts") |
| `password` | string | Yes | Archive password (usually "online-fix.me") |
| `fileName` | string | Yes | Filename in `fixes/` folder |
| `dateAdded` | string | Yes | ISO date (YYYY-MM-DD) |

---

## How to Add a Fix

### Step 1: Collect the Fix

1. Go to online-fix.me (or other source)
2. Download the fix archive for a game
3. Note: game name, AppId, archive password

### Step 2: Add to GitHub Repo

1. Clone or update `Gab-lutang/gabluchi-fixes`
2. Copy the archive to `fixes/` folder
3. Rename to `{game_name}_{appId}.rar` (e.g., `gang_beasts_123456.rar`)
4. Add entry to `index.json`
5. Commit + push

### Step 3: Verify

- GabLuchi auto-fetches the updated index
- New fix appears in MultiplayerFix search
- Users can download and apply

---

## GabLuchi Client Integration

### New Files

| File | Purpose |
|---|---|
| `GabLuchi.Services/GitHubFixService.cs` | Fetch index.json, download archives |
| `GabLuchi.Models/GitHubFixEntry.cs` | Data model |

### Updated Files

| File | Change |
|---|---|
| `GabLuchi.ViewModels/MultiplayerFixViewModel.cs` | Merge peronDepot + GitHub results |
| `GabLuchi.Views/MultiplayerFixView.xaml` | Source badge (perondepot vs github) |
| `GabLuchi/App.cs` | Register GitHubFixService in DI |
| `GabLuchi/Resources/Strings.resx` | Add GitHubFix_* strings |

### GitHubFixService.cs

```csharp
public class GitHubFixService
{
    private const string IndexUrl =
        "https://raw.githubusercontent.com/Gab-lutang/gabluchi-fixes/main/index.json";

    private const string FixesBaseUrl =
        "https://raw.githubusercontent.com/Gab-lutang/gabluchi-fixes/main/fixes/";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<List<GitHubFixEntry>> SearchAsync(string query)
    {
        var index = await FetchIndexAsync();
        var q = query.ToLowerInvariant();
        return index.Where(f =>
            f.GameName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            f.AppId.ToString() == q
        ).ToList();
    }

    public async Task<bool> DownloadFixAsync(GitHubFixEntry entry, string destPath)
    {
        using var http = new HttpClient();
        var bytes = await http.GetByteArrayAsync(entry.DownloadUrl);
        await File.WriteAllBytesAsync(destPath, bytes);
        return true;
    }

    private async Task<List<GitHubFixEntry>> FetchIndexAsync()
    {
        using var http = new HttpClient();
        var json = await http.GetStringAsync(IndexUrl);
        return JsonSerializer.Deserialize<List<GitHubFixEntry>>(json, JsonOpts) ?? new();
    }
}
```

### Merge Logic

```csharp
// In MultiplayerFixViewModel.cs
var peronResults = await _onlineFixService.SearchAsync(query);
var githubResults = await _gitHubFixService.SearchAsync(query);

// Dedupe: prefer perondepot if same AppId exists
var merged = peronResults
    .Concat(githubResults.Where(g => !peronResults.Any(p => p.AppId == g.AppId)))
    .ToList();
```

### Download Flow

```
User clicks "Apply" on a GitHub fix
        ↓
Download .rar from GitHub (raw URL)
        ↓
Save to temp folder
        ↓
Extract with 7za.exe (password from index.json)
        ↓
Apply fix files to game directory
        ↓
Done
```

---

## UI Changes

### MultiplayerFixView.xaml

Add source badge to each result:

```
┌─────────────────────────────────────────────┐
│ Gang Beasts          AppId: 123456    [Apply]│
│ [perondepot]                                │
│                                              │
│ Shieldwall           AppId: 1216320   [Apply]│
│ [gabluchi-fixes]                             │
└─────────────────────────────────────────────┘
```

### Source Badges

| Source | Badge Color | Meaning |
|---|---|---|
| `perondepot` | Blue | From online mirror (auto) |
| `gabluchi-fixes` | Green | From GabLuchi GitHub repo (curated) |

---

## Benefits

| Feature | GitHub Fixes |
|---|---|
| Infrastructure | None — just a GitHub repo |
| Cost | Free |
| File size limit | 50 MB per file (GitHub limit) |
| Bandwidth | Unlimited (raw.githubusercontent.com) |
| Auth | None (public repo) |
| Search | Client-side (fetch index, filter locally) |
| Offline | Works after first fetch (cached) |
| Spam risk | None |

---

## Limitations

| Limitation | Workaround |
|---|---|
| 50 MB per file (GitHub limit) | Most fixes are 5-20 MB. For larger fixes, use GoFile or split archive. |
| Manual index updates | Edit index.json + push. Could automate with a script later. |
| No auto-discovery | GabLuchi must be updated to fetch from GitHub. |

---

## Build Plan

| Step | What | Time |
|---|---|---|
| 1 | Create `Gab-lutang/gabluchi-fixes` repo | 5 min (manual) |
| 2 | Add test fixes to repo | 10 min (manual) |
| 3 | `GitHubFixEntry.cs` — data model | 5 min |
| 4 | `GitHubFixService.cs` — fetch + download | 20 min |
| 5 | Update `MultiplayerFixViewModel.cs` — merge logic | 15 min |
| 6 | Update `MultiplayerFixView.xaml` — source badge | 10 min |
| 7 | Update `App.cs` — DI registration | 5 min |
| 8 | Update `Strings.resx` — string resources | 5 min |
| 9 | Build + test | 15 min |

**Total: ~1.5 hours**

---

## Future Additions

### Phase 2: Local Library

- Users add their own fixes locally
- Stored in `%LocalAppData%\GabLuchi\fixes\`
- Three sources: perondepot + GitHub + local

### Phase 3: Export/Import

- Export local library as zip
- Share with friends
- Import into GabLuchi

### Phase 4: Auto-Download

- GabLuchi fetches fixes on first launch
- Caches index locally
- Periodic refresh

### Phase 5: Fix Versioning

- Track fix versions in index.json
- Notify users of updates
- Auto-download new versions

---

## Status

- [ ] Create `Gab-lutang/gabluchi-fixes` repo
- [ ] Add test fixes (Gang Beasts, Shieldwall)
- [ ] Build `GitHubFixService.cs`
- [ ] Build `GitHubFixEntry.cs`
- [ ] Update `MultiplayerFixViewModel.cs`
- [ ] Update `MultiplayerFixView.xaml`
- [ ] Update `App.cs` DI
- [ ] Update `Strings.resx`
- [ ] Build + test
- [ ] Release
