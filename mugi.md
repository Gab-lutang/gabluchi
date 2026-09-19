# Mugi Launcher v2.3.0 — Reverse Engineering Data

## Overview
- **Framework**: Qt6 C++ native app (24.3 MB PE x64)
- **Compile date**: Sep 4, 2026
- **User-Agent**: `manilua-desktop/2.3.0 (Windows)`
- **Language**: German-first, English mixed
- **Website**: https://mugi.store
- **Discord**: https://discord.gg/xNJ6w9Hqey
- **Bundled DLL**: zstd.dll (compression)
- **Binary sections**: 8 PE sections, `.text` + `.rdata` + garbled section names (likely UPX or custom packer)

## License/Tier System
Three tiers: **FREE**, **PREMIUM**, **EPIC**

Authentication via `POST /api/plugin/key/info`:
- Header: `X-Requested-With: manilua-plugin`
- Header: `x-online-key: <license_key>`
- Header: `x-device-fingerprint: <hwid>`
- Base URL: `https://mugi.store`
- User-Agent: `manilua-desktop/2.3.0 (Windows)`

**FREE**: Basic features (library, install/uninstall, search)
**PREMIUM**: Online fixes, game fixes, re-download LUA
**EPIC**: Denuvo bypass tokens (weekly limit), all PREMIUM features

## Backend API Endpoints

| Endpoint | Method | Auth | Purpose |
|---|---|---|---|
| `api/plugin/key/info` | POST | License key | Authenticate, get tier info |
| `api/plugin/online-games/list` | GET | Bearer | List online-capable games |
| `api/plugin/d-games/public-list` | GET | Bearer | List Denuvo games |
| `api/plugin/d-games/list` | GET | Bearer | Search Denuvo games |
| `api/plugin/d-games/{appid}/public-availability` | GET | Bearer | Check Denuvo token availability |
| `api/plugin/game-fix/list` | GET | Bearer | List available game fixes |
| `api/plugin/game/{appid}` | GET | Bearer | Get per-game data |
| `api/plugin/online-files/{appid}` | GET | Bearer | Get online fix download URL (EPIC) |
| `api/version/desktop` | GET | None | Version check (`MugiLauncher/2.3.0`) |
| `api/desktop/broadcast` | GET | None | Announcements/ads |
| `api/appdetails` | GET | None | Steam app details |
| `api/storesearch/` | GET | None | Steam search |

## External Game Database
- Local: `mugi_games_db.json`
- Remote: `https://generator.ryuu.lol/files/games.json`

## Downloadable Resources from mugi.store

| URL | Content | Notes |
|---|---|---|
| `mugi.store/download/plugin.zip` | Millennium Mugi plugin | Extracts to `millennium/plugins/Mugi` |
| `mugi.store/download/ost.zip` | OpenSteamTool | Extracts to Steam folder root |
| `mugi.store/download/ST.exe` | SteamTools installer | EXE, run with `-Wait`, migrates lua→stplug-in |
| `mugi.store/download/cloud_redirect.dll` | Cloud sync fix DLL | Validated MZ header before install |
| `mugi.store/download/opensteamtool.toml` | OST config | `[lua] paths = ["config\\lua"]` |
| `mugi.store/download/unrar` | Custom unrar binary | Saved as `mugi_unrar.exe` |
| `mugi.store/millennium.ps1` | Plugin install script | PowerShell |

## Online Fix Mechanism (Core)

### The Spacewar AppID 480 Trick
Online-fix.me enables multiplayer by making cracked games impersonate **Spacewar (AppID 480)**, a free Valve developer tool. Steam sees a free game, provides full multiplayer infrastructure (lobbies, P2P, invites).

### How It Works Step-by-Step
1. Game launches → `winmm.dll` (proxy loader) bootstraps the crack
2. `steam_api64.dll` is loaded → intercepts all Steam API calls
3. `ISteamUtils::GetAppID` → returns `480` (Spacewar) instead of real game ID
4. `ISteamApps::BIsSubscribedApp` → always returns `true`
5. Reports AppID 480 to Steam → Steam provides multiplayer services
6. `OnlineFix64.dll` hooks `EOS_Connect_Login` for Epic Online Services games
7. Real AppID injected into lobby metadata so players find each other
8. Players connect via Steam lobbies/invites

### Files Installed Per Game
- `steam_api64.dll` — patched, reports AppID 480 to Steam
- `OnlineFix64.dll` — core multiplayer hook DLL
- `winmm.dll` — proxy loader/bootstrap entry point
- `OnlineFix.ini` — contains `RealAppId=<actual_game_id>`
- Sometimes: `winhttp.dll`, `SteamOverlay64.dll`, `dnet.dll`, `dlllist.txt`
- Sometimes: `steam_appid.txt` containing `480`

### Download Flow (Mugi's Method)
1. Call `/api/plugin/online-files/{appid}` (requires EPIC key)
2. Server returns download URL (points to online-fix.me archives)
3. Download RAR archive
4. Extract with `mugi_unrar.exe x -ponline-fix.me -y <archive>`
5. Drop files into game install directory

### Archive Format
- **Format**: WinRAR `.rar` archives
- **Password**: `online-fix.me` (universal for all archives)
- **Distribution**: Single archive or multi-part (`.part1.rar`, `.part2.rar`)

## Embedded PowerShell Scripts

### install_mugi_plugin
```
Kill Steam → Download plugin.zip → Extract to millennium/plugins/Mugi → Restart Steam
```
- Plugin dir: `Steam\millennium\plugins\Mugi`
- Deletes old plugin folder first

### install_opensteamtool
```
Kill Steam → Download ost.zip → Extract to Steam folder → Restart Steam
```

### install_steamtools
```
Kill Steam → Download ST.exe → Run installer (-Wait) → 
Migrate configs from config\lua → config\stplug-in → Restart Steam
```

### fix_opensteamtool
```
Kill Steam → Download cloud_redirect.dll → Validate MZ header (0x4D 0x5A) →
Move to Steam folder → Restart Steam
```

### fix_opensteamtool_toml
```
Kill Steam → Download opensteamtool.toml → Move to Steam folder → Restart Steam
```

### update_mugi_plugin
Same as `install_mugi_plugin` (force reinstall)

### Common Pattern in All Scripts
```powershell
$steamPath = 'C:\Program Files (x86)\Steam'
# Registry fallback
$regPath = (Get-ItemProperty -Path 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam').InstallPath
# Kill Steam
Stop-Process -Name 'steam' -Force
Start-Sleep -Seconds 2
# ... do work ...
# Restart Steam
Start-Process -FilePath $steamExe
```

## Troubleshooting System
Auto-detects missing files in Steam folder:
- `opensteamtool.dll` — "Games might show as 'Purchase' or won't show in library"
- `cloud_redirect.dll` — "Cloud sync errors and cloud saves may not work"
- `opensteamtool.toml` — "Games won't show in library"

Each has a one-click "Fix" button that runs the corresponding PowerShell script.

## UI Structure

### Sidebar Navigation
- Account (login/license)
- Library (game tiles with cover art from Steam CDN)
- Online (online games list)
- Denuvo (Denuvo token downloads — EPIC only)
- Fixes (game-specific fixes)
- Troubleshoot (auto-detect + fix)
- Plugins (Mugi plugin for Millennium)
- Settings

### Game Detail Panel Actions
- Redownload LUA
- Open Folder
- Steam Store
- **Download Online Fix** (EPIC/Premium)
- **Download Token** (EPIC only — Denuvo)
- **Download Fix**
- Delete Game

### Sidebar Styling
- Collapsible sidebar (Ctrl+B)
- Brand card: "MUGI" mark, "DESKTOP SUITE" name, "WORKSPACE" subtitle
- Key type badge: FREE / ACCESS / PREMIUM / EPIC
- Section labels: "Add", "Manage", "Online", "Denuvo", "Fixes", "Troubleshoot"

## CDN Sources for Game Headers
- `cdn.akamai.steamstatic.com/steam/apps/{id}/header.jpg`
- `cdn.cloudflare.steamstatic.com/steam/apps/{id}/header.jpg`
- `cdn.steamstatic.com/steam/apps/{id}/header.jpg`
- `shared.fastly.steamstatic.com/store_item_assets/steam/apps/{id}/header.jpg`
- `store.steampowered.com/api/appdetails?appids={id}`

## Token System (Denuvo)
- Weekly download limit per token
- Format: `{appid}_token.zip`
- Installed to game folder
- Rate limit field: `token_weekly_limit`
- Requires EPIC tier key

## Windows Defender Handling
Script disables real-time protection via admin elevation:
```powershell
Start-Process powershell -Verb RunAs -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',
  'Set-MpPreference -DisableRealtimeMonitoring $true; Add-MpPreference -ExclusionPath ...'
```

## Registry Paths Accessed
- `HKEY_CURRENT_USER\Software\...` (license key storage)
- `HKEY_LOCAL_MACHINE\Software\...` (Steam path detection)
- `HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam` (Steam install path)

## Key Strings Found in Binary
- `manilua-plugin` — X-Requested-With header value
- `manilua-desktop/2.3.0` — User-Agent
- `mugi_xxxxxxxxxxxxxxx` — Likely HWID format
- `token_weekly_limit` — Denuvo token rate limit
- `code` — API response field
- `success` / `error` — API response status
- `Bearer ` — Auth prefix for game APIs
- `OnlineFix.ini` — Config file with RealAppId
- `online-fix.me` — RAR password
- `Expand-Archive` — PowerShell extraction
- `-ponline-fix.me` — unrar password flag
- `mugi_unrar.exe` — Custom unrar binary name
- `millennium` — Steam plugin framework
- `opensteamtool.dll` — Game unlock DLL
- `cloud_redirect.dll` — Cloud sync fix
- `steam_api64.dll` / `steam_api.dll` — Target DLLs for replacement
- `winmm.dll` — Proxy loader DLL
- `480` — Spacewar AppID used for online spoofing
