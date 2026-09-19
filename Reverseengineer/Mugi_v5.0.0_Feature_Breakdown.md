# Mugi v5.0.0 Feature Breakdown

## Overview
Major update to MugiLauncher (mugi.store) — Steam library integration, cloud saves, social discovery, version selection. Released alongside "Mugi Cloud" for Epic members.

## Architecture
- **Framework**: Qt6 C++ native app (24.3 MB PE x64)
- **Plugin**: Millennium Steam plugin (JavaScript UI inside Steam client)
- **Backend API**: `https://mugi.store/api/plugin/...`
- **Cloud storage**: Cloudflare R2
- **Social**: Discord OAuth2 integration

---

## Feature 1: Steam Library Button Integration

### What It Does
A "Mugi" button appears next to "Play" on a game's Steam Library page. Opens a modal with per-game actions.

### Actions Available
- Add game to library
- Add available Online support
- Install available fixes
- Request a Denuvo token
- View/copy game AppID
- Change Mugi settings
- Run Health Check
- Open MugiPlay

### Status Display in Button
- Key type (FREE / PREMIUM / EPIC)
- Key expiry date
- Activity status
- Daily/weekly usage limits

### Technical Implementation
- **Millennium plugin** injects into Steam's React-based library UI
- `Millennium.add_browser_js()` loads the plugin's JavaScript
- JavaScript creates DOM elements and attaches to Steam's library page
- Communication via `Millennium.callServerMethod()` → Lua backend → HTTP API

### GabLuchi Parity
✅ **Already implemented** — `gabluchi.js` injects a GabLuchi button into Steam library. Our RPC bridge (`main.lua`) relays to the desktop app's HTTP API.

---

## Feature 2: Version Selector (Build Downloader)

### What It Does
Epic members can install older game builds directly from the Library menu.

### Capabilities
- Browse available builds with release dates
- Enter a custom manifest ID
- "Always up to date" option to return to latest
- Non-Epic members see pinned version with labels

### Technical Implementation
- Backend queries Steam's depot manifest history (likely via SteamKit2 or similar)
- Manifest IDs map to specific game builds
- Download flow: manifest ID → depot download → extract → replace game files
- Requires access to Steam's content servers with valid ticket

### GabLuchi Parity
❌ **Not implemented** — We have `ManifestPreCacheService` for pre-caching manifests, but no UI to browse/select older versions. Needs:
- New service: `ManifestHistoryService` (fetch available manifests per game)
- New RPC: `GetAvailableVersions(appid)`, `InstallVersion(appid, manifestId)`
- New JS UI: version dropdown in game menu

---

## Feature 3: Mugi Cloud (Cloud Saves)

### What It Does
Secure cloud-save synchronization across registered PCs for Epic members.

### Features
- Cloud saves on/off toggle per game
- Choose which PCs can sync
- Storage usage display
- Supported saves sync across devices
- Disabled by default (user opt-in)

### Technical Implementation
- **Storage**: Cloudflare R2 (S3-compatible)
- **Free tier**: 500 MB per Epic key (Mugi's own limit, not R2's)
- **Paid tier**: Extra 1 GB/year for $9.99
- **Sync**: Per-game save directory upload/download
- **Device registration**: PCs registered to account, user selects sync targets

### R2 Actual Pricing vs Mugi's Pricing
| | Mugi | GabLuchi (using R2 directly) |
|---|---|---|
| Free storage | 500 MB per key | **10 GB total** (R2 free tier) |
| Paid storage | $9.99/yr for +1 GB | $0.015/GB/mo (pay only what you use) |
| Egress | Unknown | **FREE always** |
| Per-user cost | Users pay $9.99/yr | **$0** for first ~200 users |

Mugi is essentially reselling R2 storage at a massive markup. We can offer the same feature for free.

### GabLuchi Parity
❌ **Not implemented** — Needs:
- New service: `CloudSaveService` (Cloudflare R2 client via AWSSDK.S3)
- New RPC: `GetCloudStatus(appid)`, `UploadSave(appid)`, `DownloadSave(appid)`, `ListDevices()`
- New JS UI: cloud sync toggle, storage meter
- Desktop app: Cloudflare R2 credentials, save directory detection per game

---

## Feature 4: MugiPlay (Social Discovery)

### What It Does
Optional social layer for connecting with Mugi friends and discovering games.

### Features
- Connect with Mugi friends
- See games friends recently added
- Discover games through artwork-rich cards
- Add discoveries to library with one click
- See which games you already own
- Copy/share MugiPlay friend ID
- Uses Discord display names + profile pictures
- Can be enabled/disabled

### Technical Implementation
- **Auth**: Discord OAuth2
- **Data**: Discord friends list filtered to Mugi users
- **API**: `mugi.store/api/plugin/...` endpoints for friend activity
- **UI**: Sidebar or overlay panel in Steam library

### GabLuchi Parity
❌ **Not implemented** — Needs:
- Discord OAuth2 app registration
- New service: `SocialService` (Discord API + friend activity)
- New RPC: `GetFriends()`, `GetRecentGames()`, `GetDiscoveries()`
- New JS UI: friends sidebar, game discovery cards

---

## Feature 5: Health Check + AV Warning

### What It Does
Detects missing/outdated Mugi support files and installs verified replacements. Warns when antivirus removes Online Fix DLLs.

### Capabilities
- Auto-detect missing/corrupted DLLs
- Install verified replacements on update
- Warn before game/online mode breaks from AV removal

### Technical Implementation
- Manifest of expected files with SHA256 hashes
- Periodic check on plugin load
- Comparison against `health-manifest` endpoint (`mugi.store/api/plugin/health-manifest`)
- Warning UI before game launch if files missing

### GabLuchi Parity
❌ **Not implemented** — Needs:
- New RPC: `HealthCheckGame(appid)`, `RepairGame(appid)`
- Expected files manifest per game (OnlineFix64.dll, steam_api64.dll, winmm.dll, etc.)
- Hash verification against known-good values
- JS UI: health badge per game, warning popup

---

## Feature 6: UI Refresh

### What's New
- Smooth animations
- Clearer buttons with icons
- Loading placeholders
- Progress ring with success tick / failure cross
- Improved messages

### GabLuchi Parity
⚠️ **Partially implemented** — Our plugin has themes (11 CSS themes) and gamepad navigation. But the UI could use modernization:
- Animated transitions
- Better loading states
- Icon buttons
- Progress indicators

---

## Comparison Matrix

| Feature | Mugi v5.0.0 | GabLuchi Plugin v1.0.4 |
|---|---|---|
| Library button | ✅ | ✅ |
| Add game | ✅ | ✅ |
| Online fix | ✅ | ✅ |
| Fixes menu | ✅ | ✅ |
| Settings in plugin | ✅ | ✅ |
| Game folder | ✅ | ✅ |
| Multi-language | ✅ | ✅ (AR, BG, EN, PT) |
| Themes | ✅ | ✅ (11 themes) |
| Big Picture/gamepad | ❌ | ✅ |
| Version selector | ✅ | ❌ |
| Cloud saves | ✅ | ❌ |
| MugiPlay social | ✅ | ❌ |
| Health Check + AV | ✅ | ❌ |
| Modern UI refresh | ✅ | ⚠️ (partial) |

---

## Priority Order for GabLuchi Implementation

1. **Health Check + AV Warning** — Low effort, high impact, prevents broken games
2. **Version Selector** — Unique feature, uses existing manifest infrastructure
3. **Cloud Saves** — Nice to have, Cloudflare R2 is cheap
4. **UI Refresh** — Polish existing features
5. **MugiPlay Social** — Most complex, optional
