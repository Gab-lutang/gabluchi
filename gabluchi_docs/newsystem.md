# Manifest System — Research & Plan

## Problem
The manifest backend at `167.235.229.108` has a **global rate limit of ~25 requests** (shared across ALL GabLuchi users, not per-IP). When users add different games simultaneously, everyone hits the limit. The server is also behind Cloudflare which blocks CF Worker proxy requests (returns 403).

## Current Architecture

### What hits the manifest backend
| Request | Endpoint | Count per "Add Game" |
|---------|----------|---------------------|
| Source availability check | `GET /check_apis?appid=X` | 1 |
| Manifest download | `GET /{appid}` (via license redirect) | 1 |
| **Total per game** | | **2** |

### Request flow
1. `GabLuchiApiClient.CheckSourcesAsync()` → `GET http://167.235.229.108/check_apis?appid=X` (UA: `secretgoonpoon`)
2. User picks source → `ManifestDownloader.DownloadManifestAsync()`
3. Hits license gate: `GET {KeyCheckerBase}/manifest/{appid}?token={jwt}` → returns `{"url": "http://167.235.229.108/{appid}"}`
4. Downloads ZIP from manifest server
5. ZIP contains `.lua` + `.manifest` files → installed to `Steam/config/lua/` and `Steam/depotcache/`

### Why CF Worker proxy doesn't work
- `167.235.229.108` is behind Cloudflare (`Server: cloudflare` in response headers)
- CF Workers fetching a CF-proxied origin → 403 Forbidden
- We don't control the manifest server's Cloudflare dashboard (scraped from lua.tools)
- Cannot whitelist CF Worker IPs

### Sources in fallback list
| Source | URL | Format | Rate Limited |
|--------|-----|--------|-------------|
| Ryuu | `http://167.235.229.108/<appid>` | ZIP (.lua + .manifest) | YES (shared bucket) |
| Sushi | `raw.githubusercontent.com/sushi-dev55-alt/sushitools-games-repo-alt/.../<appid>.zip` | ZIP (.lua + .manifest) | NO (GitHub CDN) |
| Luie | `http://167.235.229.108/<appid>` | ZIP (.lua + .manifest) | YES (same server as Ryuu) |

**Critical: Sushi is never actually used** because:
1. `/check_apis` only reports Ryuu and Luie as available — not Sushi
2. License gate always redirects back to the manifest server, not Sushi
3. Sushi is last priority in the fallback list

## Existing GitHub Mirror Ecosystem

### Actively maintained repos (community-run, free)
| Repo | Last Updated | Size | Content |
|------|-------------|------|---------|
| `Sainan/k25FCdfEOoEJ42S6` | Aug 15, 2026 | ~60 GB | Raw `.manifest` binary files (per-depot) |
| `steamtools-games/ManifestHub3` | Aug 10, 2026 | ~21 GB | `.lua` + `key.vdf` files (one git branch per appid) |
| `SteamAutoCracks/ManifestHub` | Jul 16, 2026 | ~32 MB | `depotkeys.json` + `appaccesstokens.json` |
| `sushi-dev55-alt/sushitools-games-repo-alt` | **Nov 2025 (stale)** | ~6 GB | Pre-packaged ZIPs (.lua + .manifest per appid) |

### Sushi repo details
- ~10,934 games (one ZIP per commit)
- Format matches manifest server exactly: ZIP → `.lua` + `.manifest` + readme
- Last updated Nov 18, 2025 — 10 months stale
- No new games added since then
- Popular games present: CS2, GTA V, Elden Ring, Cyberpunk, RDR2, BG3, Stardew Valley, Palworld, Wukong, Satisfactory
- Some missing: Lethal Company

### How manifest repos get populated
Tool: `Auiowu/ManifestAutoUpdate` (GitHub Actions + cron)
1. Login to Steam via SteamKit2/ValvePython (requires Steam account with game ownership)
2. Discover owned apps via license parsing
3. Check for manifest GID changes against stored values
4. Fetch manifests from Steam CDN using manifest request codes
5. Decrypt filenames using depot keys
6. Package and push to GitHub
7. **Requires: Steam account with games purchased — LO doesn't have this**

### Other alternatives considered
| Service | Format | Rate Limit | DLC Support | Verdict |
|---------|--------|-----------|-------------|---------|
| steamtools.games | `.lua` + `key.vdf` | 1 req/1.5s | No | **Incompatible** — no `.manifest` files, clients without OST can't use it |
| lua.tools | DLC info + Denuvo | Auth required | Yes | **Different service** — DLC/Denuvo only, not manifest downloads |
| ManifestDeX | Manifests + depot keys | 100 pts/day | No | Possible backup, limited daily quota |
| Hubcap | Manifests | Requires Discord role + API key | No | Paid/premium, already integrated |

## Plan Options

### Option A: Hybrid approach (simplest, no new infrastructure)
1. **Cache `/check_apis` responses** — in-memory, 5-min TTL per appid
2. **Make Sushi a direct-download source** — skip license gate for GitHub URLs
3. **Reorder sources** — Sushi first, manifest server as fallback
4. **Result:** Most popular games download from GitHub (no rate limit), only unknown/new games hit the manifest server

Changes needed:
- `GabLuchiApiClient.cs` — add Dictionary<string, (Dictionary<string,string>, DateTime)> cache for CheckSourcesAsync
- `ManifestDownloader.cs` — detect GitHub URLs and skip license gate
- `ManifestDownloader.cs` + `HttpServerService.cs` — reorder fallback sources (Sushi first)

### Option B: Full GitHub mirror (best long-term, needs Steam account)
- Set up ManifestAutoUpdate as GitHub Actions cron
- Requires Steam account with game library
- LO doesn't have money for games → **not viable**

### Option C: Use k25FCdfEOoEJ42S6 + ManifestHub3 directly
- k25FCdfEOoEJ42S6: raw `.manifest` files (actively maintained, updated weekly)
- ManifestHub3: `.lua` files (one branch per appid)
- Client already has `ManifestPreCacheService` that fetches from k25FCdfEOoEJ42S6
- Would need to add ManifestHub3 `.lua` fetching
- More complex but fully automated by community

## Files involved
- `GabLuchi\Config.cs` — `ManifestBackendBase` default (line 48)
- `GabLuchi.Services\GabLuchiApiClient.cs` — `CheckSourcesAsync()` (line 89-99)
- `GabLuchi.Services\ManifestDownloader.cs` — `DownloadManifestAsync()` (line 51-90), fallback sources (line 214-219)
- `GabLuchi.Services\HttpServerService.cs` — `HandleCheckSources()` (line 581-606), fallback sources (line 108-113)
- `GabLuchi.Services\ManifestPreCacheService.cs` — already fetches from k25FCdfEOoEJ42S6
- `GabLuchi\AppConfig.cs` — `GitHubMirrorBaseUrl` (line 64)
- `GabLuchi.config.json` — runtime config override

## Recommendation
**Start with Option A** — it's the smallest change and solves the immediate rate limit problem. Option C can be explored later as a more comprehensive solution.
