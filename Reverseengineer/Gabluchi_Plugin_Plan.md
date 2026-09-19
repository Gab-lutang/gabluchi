# GabLuchi Plugin Revival Plan

## Current State
- **Repo**: https://github.com/Gab-lutang/gabluchi-plugin
- **Version**: v1.0.4 (9 commits, last: Aug 14 2026)
- **Status**: Not maintained, but fully functional architecture
- **Framework**: Millennium Steam plugin (Lua backend + JS frontend)

## Architecture Summary
```
User clicks button in Steam library
  → gabluchi.js (injected JS) calls Millennium.callServerMethod()
  → main.lua (Lua backend) receives RPC
  → main.lua calls GabLuchi app HTTP API (127.0.0.1:6767)
  → GabLuchi desktop app does the work
  → Response flows back to JS → UI update
```

## Existing RPC Endpoints (Desktop App → 127.0.0.1:6767)

| Endpoint | Method | Purpose |
|---|---|---|
| `GET /has/{appid}` | HasGabLuchiForApp | Check if game in library |
| `POST /add/{appid}` | StartGabLuchiAdd | Add game |
| `GET /add-status/{appid}` | GetGabLuchiAddStatus | Poll add progress |
| `POST /add-source/{appid}` | PickGabLuchiAddSource | Pick download source |
| `POST /check-sources/{appid}` | CheckApisForApp | Search fix sources |
| `POST /download/{appid}` | StartAddViaGabLuchiFromUrl | Download fix |
| `GET /download-status/{appid}` | GetAddViaGabLuchiStatus | Poll download progress |
| `POST /cancel/{appid}` | CancelAddViaGabLuchi | Cancel download |
| `POST /remove/{appid}` | DeleteGabLuchiForApp | Remove from library |
| `POST /open/fix/{appid}` | OpenFix | Apply online fix |
| `POST /open/settings` | OpenSettings | Open settings |
| `POST /restart-steam` | RestartSteam | Restart Steam |
| `GET /loaded-apps` | ReadLoadedApps | "Games added" popup |
| `POST /loaded-apps` | DismissLoadedApps | Dismiss popup |

---

## Phase 1: Health Check + AV Warning

### Goal
Detect missing/corrupted multiplayer fix DLLs and warn users before games break.

### New Desktop App Endpoints

| Endpoint | Method | Purpose |
|---|---|---|
| `GET /health/{appid}` | HealthCheckGame | Check fix file integrity |
| `POST /health/{appid}/repair` | RepairGame | Re-download missing files |

### Health Check Logic
```
1. Get game install path (Steam library)
2. Read OnlineFix.ini → get expected files
3. Check each file exists + SHA256 matches
4. Return status: { healthy: bool, missing: [], corrupted: [] }
```

### Expected Files Per Game
```json
{
  "files": [
    { "name": "steam_api64.dll", "required": true },
    { "name": "OnlineFix64.dll", "required": true },
    { "name": "winmm.dll", "required": false },
    { "name": "OnlineFix.ini", "required": true }
  ]
}
```

### New JS UI Elements
- **Health badge** on game card: 🟢 (healthy) / 🟡 (warning) / 🔴 (broken)
- **Warning popup** when AV removes DLLs: "OnlineFix64.dll was removed by [AV name]. Online mode won't work."
- **Repair button** in game menu: "Repair Fix Files"

### New Lua RPC
```lua
function HealthCheckForApp(appid)
    return backend_request("GET", "/health/" .. tostring(appid))
end

function RepairFixForApp(appid)
    return backend_request("POST", "/health/" .. tostring(appid) .. "/repair")
end
```

### Estimated Effort
- Desktop app: 2-3 hours (endpoint + hash verification)
- Plugin Lua: 30 min (2 RPC handlers)
- Plugin JS: 2-3 hours (UI elements + popups)
- **Total: ~1 day**

---

## Phase 2: Version Selector (Manifest Browser)

### Goal
Let users browse and install specific game versions by manifest ID.

### New Desktop App Endpoints

| Endpoint | Method | Purpose |
|---|---|---|
| `GET /manifests/{appid}` | GetManifests | List available versions |
| `POST /manifests/{appid}/{manifestId}` | InstallManifest | Download specific version |

### Manifest History Source
- SteamKit2 depots API → fetch depot manifest history
- Cache locally to avoid repeated API calls
- Return: `[{ manifestId, date, size, notes }]`

### New JS UI Elements
- **Version dropdown** in game menu: list of available builds
- **Custom manifest input**: text field for manual manifest ID entry
- **"Always up to date" toggle**: returns to latest version
- **Download progress**: progress bar during version download

### New Lua RPC
```lua
function GetManifestsForApp(appid)
    return backend_request("GET", "/manifests/" .. tostring(appid))
end

function InstallManifestForApp(appid, manifestId)
    return backend_request("POST", "/manifests/" .. tostring(appid) .. "/" .. tostring(manifestId))
end
```

### Estimated Effort
- Desktop app: 4-5 hours (SteamKit2 integration + download logic)
- Plugin Lua: 30 min (2 RPC handlers)
- Plugin JS: 3-4 hours (UI dropdown + progress)
- **Total: ~1.5 days**

---

## Phase 3: Cloud Saves

### Goal
Sync game saves across PCs via Cloudflare R2.

### Pricing Reality (10 users)
- **Cloudflare R2 free tier**: 10 GB storage, 1M writes/mo, 10M reads/mo — **permanent, not a trial**
- **Your projected usage**: 11 users × ~150 MB avg = **~1.65 GB** (16% of free tier)
- **Cost: $0** — you won't pay anything until 200+ users
- **Mugi charges $9.99/yr** for extra storage. We offer it free.

### Per-User Storage Estimates
| User Type | Games Synced | Storage |
|---|---|---|
| Casual (2-3 games) | Small/indie saves | ~30-80 MB |
| Regular (5-10 games) | Mix of AAA + indie | ~100-300 MB |
| Power user (15+ games) | Heavy saves, large games | ~300-500 MB |
| You (heavy gamer) | Tons of games | ~500 MB - 1 GB |

### New Desktop App Endpoints

| Endpoint | Method | Purpose |
|---|---|---|
| `GET /cloud/{appid}/status` | GetCloudStatus | Check sync status |
| `POST /cloud/{appid}/upload` | UploadSave | Upload saves to cloud |
| `POST /cloud/{appid}/download` | DownloadSave | Download saves from cloud |
| `GET /cloud/devices` | ListDevices | List registered PCs |
| `POST /cloud/{appid}/toggle` | ToggleCloudSync | Enable/disable sync |

### Storage Architecture
- **Provider**: Cloudflare R2 (S3-compatible, $0.015/GB after free tier)
- **SDK**: `AWSSDK.S3` with R2 endpoint swap (no new dependencies)
- **Structure**: `saves/{appid}/{deviceId}/{timestamp}/`
- **Free tier**: 10 GB (covers ~200 casual users or ~50 power users)
- **Conflict resolution**: Latest timestamp wins, keep 3 backups

### Save Directory Detection
- Steam's `userglob` field in `appmanifest_*.acf`
- Common paths: `%USERPROFILE%/Documents/My Games/`, `%LOCALAPPDATA%/`, `%APPDATA%/`
- Per-game mapping in `GamePortLookup.cs` (extend with save paths)

### New JS UI Elements
- **Cloud sync toggle** per game (on/off)
- **Storage meter**: "245 MB / 10 GB used" (free tier display)
- **Last synced**: timestamp display
- **Sync now button**: manual trigger

### New Lua RPC
```lua
function GetCloudStatusForApp(appid)
    return backend_request("GET", "/cloud/" .. tostring(appid) .. "/status")
end

function ToggleCloudForApp(appid, enabled)
    return backend_request("POST", "/cloud/" .. tostring(appid) .. "/toggle", { enabled = enabled })
end
```

### Cost Projections
| Users | Storage | Monthly Cost |
|---|---|---|
| 10 (current) | ~1.65 GB | **$0** |
| 50 | ~7.5 GB | **$0** |
| 100 | ~15 GB | ~$0.08 |
| 200 | ~30 GB | ~$0.30 |
| 500 | ~75 GB | ~$0.98 |
| 1,000 | ~150 GB | ~$2.10 |

### Estimated Effort
- Desktop app: 6-8 hours (R2 client + save detection + sync logic)
- Plugin Lua: 1 hour (3-4 RPC handlers)
- Plugin JS: 4-5 hours (UI toggle + storage meter)
- **Total: ~2.5 days**

---

## Phase 4: MugiPlay Social (Optional)

### Goal
Discord-based friend activity and game discovery.

### New Desktop App Endpoints

| Endpoint | Method | Purpose |
|---|---|---|
| `GET /social/friends` | GetFriends | List Mugi-using Discord friends |
| `GET /social/recent` | GetRecentGames | Games friends recently added |
| `GET /social/discover` | GetDiscoveries | Recommended games |

### Discord Integration
- **OAuth2**: Redirect to Discord → get access token
- **API**: `GET /users/@me/friends` → filter to Mugi users
- **Rich Presence**: Show "Playing [game]" in Discord

### New JS UI Elements
- **Friends sidebar**: Discord avatars + names + recent games
- **Discovery cards**: artwork-rich game cards with "Add" button
- **Friend ID display**: copy/share your MugiPlay ID

### Estimated Effort
- Desktop app: 8-10 hours (OAuth + Discord API + social endpoints)
- Plugin Lua: 1-2 hours (3-4 RPC handlers)
- Plugin JS: 6-8 hours (social sidebar + discovery cards)
- **Total: ~4 days**

---

## Implementation Order

| Phase | Feature | Effort | Impact | Cost | Dependencies |
|---|---|---|---|---|---|
| 1 | Health Check + AV Warning | ~1 day | High | $0 | None |
| 2 | Version Selector | ~1.5 days | Medium | $0 | None |
| 3 | Cloud Saves | ~2.5 days | Medium | **$0** (10 GB free tier covers 200+ users) | Cloudflare R2 account |
| 4 | UI Refresh | ~1 day | Medium | $0 | None |
| 5 | MugiPlay Social | ~4 days | Low | $0 | Discord app registration |

**Total estimated effort: ~10 days**
**Total estimated cost: $0** (all features fit within free tiers)

---

## Getting Started

### Prerequisites
1. Clone plugin repo: `git clone https://github.com/Gab-lutang/gabluchi-plugin`
2. Install Millennium: https://docs.steambrew.app/
3. Test plugin loads in Steam
4. Verify RPC bridge works (GabLuchi app must be running)

### Development Workflow
1. Edit `gabluchi.js` (frontend) or `main.lua` (backend)
2. Reload Steam (Millennium auto-reloads plugins)
3. Test in Steam library
4. When ready: tag release → CI builds plugin.zip → auto-deploy

### Testing
- Install Millennium in a test Steam instance
- Drop plugin into `Steam/millennium/plugins/gabluchi/`
- Launch Steam → verify GabLuchi button appears
- Test each RPC method against running GabLuchi desktop app
