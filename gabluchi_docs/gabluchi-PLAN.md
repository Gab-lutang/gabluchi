# GabLuchi — Feature & Reverse-Engineering Plan

Status: PLANNED (implementing after PC session)
Date: 2026-09-20 (rev 11 — DenuvOwO/voices38 scene-intel block added: RD945 audit + Habr/CRACKLAB confirm token expiry = embedded ASN.1 fake-license cert `260226065959Z→270226071959Z` (<b>2027-02-26</b>), Denuvo license read ID `92346205896` + `CreateFileW` IAT redirect to `KIRIGIRI.bin` (runtime-written, CPU-vendor-specific); built-in `0xDEADC0DE` UEFI DSE bypass (EfiGuard optional, not required); launch base = detanup01 GBE fork + HyperEvade; two generations: V2 ColdClientLoader/`steamclient_loader_x64.exe` (MKDEV-style) vs V3 VBS.cmd + Steam launch (DenuvOwO unified). Exact facts supersede the older "~Feb 2027" phrasing. **Rev 11.1 (2026-09-20):** post-lawsuit scene update — voices38 NOT dried up: 5-crack burst 2026-09-06/07 (MK1, MGSV Complete, SW Outlaws, PoP Lost Crown, P3R) after a 3-week break, then "everything will continue as normal" on r/CrackWatch; lawsuit record exact (case `5:26-cv-10423`, N.D. Cal, 2026-09-14, HG LAW LLP, two §1201 counts + jury demand, names Reddit acct + Discord user ID + 7 Steam profiles, no subpoena application yet, 26-game list already outdated at filing); **listings-API cadence correction — the feed has no timestamp fields and the Steam CDN `?t=` is header-art only, so a quiet feed ≠ a quiet scene** (derive cadence from scene news). Rev 10 root-cause/PATCH.md + rev 9 two-roads + rev 8 Mugi folds intact)
Target: Gab-lutang/gabluchi ecosystem (gabluchi WPF app, gabluchi-unlocker C++ engine, gabluchi-plugin, gabluchi-fixes, gabluchi-manifests)

---

## 1. What GabLuchi is today

A C# WinUI/WPF desktop app (.NET 7+) that acts as a **MugiLauncher-class fix manager + Lua plugin installer**, wrapped in a license dashboard, with online-multiplayer fix distribution. The Gab-lutang org already ships a full ecosystem:

- `gabluchi` — desktop app (WPF, CommunityToolkit.Mvvm). Largest services: `UnlockerService` (31 KB, orchestrates all unlock paths), `HttpServerService` (30 KB, local HTTP/RPC bridge on `127.0.0.1:6767`), `PluginInstallerService` (22 KB).
- `gabluchi-unlocker` — C++ engine (GPL-3.0). Home of future native shims/gate DLLs.
- `gabluchi-plugin` — Millennium-style Steam plugin + loader release (RPC bridge: game-lib button in Steam -> `gabluchi.js` -> `main.lua` -> desktop app HTTP API).
- `gabluchi-fixes` — community multiplayer fix distribution (used for games not on perondepot).
- `gabluchi-manifests` — manifest mirror (harvested from source servers).

Already implemented: online-fix search/download/apply (perondepot + own fixes index + CrakFiles), GitHub fixes repo, Lua plugin install (file-copy/patch scripts), CEF injection into Steam UI tabs, cloud-save redirect (OST `cloud_redirect.dll` management), Goldberg + CreamAPI + SmokeAPI + Uplay R1/R2 DLC unlockers, SteamAPICheckBypass, depot manifest download/precache, **game health checks + Defender quarantine detection** (`GameHealthService`, `BackgroundHealthScanner` — shipped, despite older docs saying "planned"), self-hosted Go relay (ConnectRelay) + lobby browser, `gabluchi://` deep links + phone companion, 11 themes, Big Picture/gamepad support (edge over Mugi), license/auth/HWID services, version-selector groundwork (80%) + manifest history infra.

**Key gap this plan closes:** GabLuchi currently patches *files* but does not *hook the Steam client*. It has none of the SteamTools/OpenSteamTool-class client-side bypass internals (IPC spoof, ticket forging, PICS/package injection, depot-key injection, rich-presence/achievements spoof, version-resilient pattern tracking). It also lacks cloud **save sync** (only redirect exists), and drops a pinned-build version browser + multi-provider manifest resilience.

Correction baked in: "MugiPlay" is **two** things — per-device recording toggle on Epic bindings (dashboard) **and** a social discovery feed (Steam MUGIPLAY tab: Mugi friends, one-click library add, activity). Earlier "NOT social" ruling was wrong; rev 7 reconfirmed both surfaces. See §3.

**Backend direction (owner decision 2026-09-19):** reuse GabLuchi's existing backend/licensing surface — do NOT build a new web stack. Live anatomy in §11 (`Config.cs` endpoints, 2 Cloudflare Workers, `lua.tools`, `hubcapmanifest.com`, own manifest server `167.235.229.108`, ConnectRelay, `127.0.0.1:6767` RPC). F-7's license gate already half-exists via `{KeyCheckerBase}/manifest/{appid}?token=`.

---

## 2. Reference ecosystem map (what we reverse-engineered)

| Tool | What it actually is | What to steal |
|---|---|---|
| **SteamTools / OpenSteamTool** | The real client-side DRM bypass core. OpenSteamTool is the modern open-source continuation (pattern-tracked, hot-reload). | AppTicket forging via SteamDRM off-by-four, IPC session spoof (`Global_SteamtoolsIPC_Class`), netpacket/eMsg spoof, PICS access-token injection, package injection, **hook-time depot-key injection** (`LoadDepotDecryptionKey`), **pin authority via `BuildDepotDependency` patching `DepotEntry.ManifestGid`**, KeyValues VDF patching, manifest pinning, rich-presence/achievements spoofs. |
| **LuaTools plugin layer** | Millennium plugin: Lua scripts in `config/lua` / `stplug-in` + `luapacka.exe` compiler, auto-loaded by a hooked DLL in the Steam root. | Live Lua scripting API (case-insensitive funcs): `addappid`, `setmanifestid`, `addtoken`, `setAppTicket`, `setETicket`, `pinApp`, `fetch_manifest_code_ex`. **Pin state machine: Active / Commented pins; Denuvo/fix slots `forceLocked`.** |
| **luatools-moon** | Linux-native stack: `LD_AUDIT`/`LD_PRELOAD` wrapper (slsteam-moon) + "Lumen" Lua sidecar + Millennium plugin frontend + CloudRedirect `.so`. | Steam Deck / Proton support path; CloudRedirect; `WINEDLLOVERRIDES` fix config. |
| **Mugi (v5.2.0)** | Server-orchestrated full stack, NOT "a plugin": Millennium Lua plugin (v5.2.0, 6,667-line `main.lua` backend) + **OpenSteamTool parked in Steam root** + `cloud_redirect.dll` + Qt6 MugiLauncher, shipped from one Cloudflare domain. Headers fully forgeable (fingerprint = hex-hash of `COMPUTERNAME-USERNAME`), persistent keys in `config\manilua`, host-redirectable backend via `api_server.txt`. Trust score 1/100 ("phishing"), 8–10 blacklists, unsigned single-CDN binaries. | **Copy:** health-manifest pattern, per-device audit toggle **+ MugiPlay social feed**, SHA-256 self-healing updater (kill/restart Steam), one-shot scheduled-task persist, buy-again-extends storage UX, transparent quotas. **Do NOT copy:** forgeable anti-tamper headers, `server_route` host-pin (MITM surface), Discord-hostage free tier, destructive Steam-root "reset", per-key cloud hostage, opaque quotas, unsigned distro, HWID-only identity. (Full appendix §3.) |
| **SteamAutoCracks/Steam-API-Check-Bypass** | Hooks `CreateFile` to redirect/hide files at nth-open. | Already shipped; reuse `file_redirect`/`file_hide` + nth-time hook pattern for the gate DLL. |
| **SteamTools troj** (`xinput1_4.dll` remote loader) | Timed-bomb supply chain: AES-256-CBC payloads from CDNs, machine-hash file obfuscation, `HttpLoadDLL`. | Anti-pattern to explicitly avoid and defend against (F-6). |

Security finding for the README: the SteamTools ecosystem carries confirmed remote-code-execution backdoors in its "loader" DLLs. GabLuchi must never pull that pattern.

---

## 3. Mugi deep-dive appendix (live site + SPA bundle + installer RE)

Companion dossier file: `infos.md` (full raw findings). Here: the decisions.

**Real components (client-installed)**: `Steam\millennium\plugins\Mugi\` (Lua plugin, tabs Home/Add/Manage/Online/Denuvo/Fixes/Settings); `Steam\OpenSteamTool.dll` + `dwmapi.dll` + `xinput1_4.dll` (the bypass, from `OpenSteam001/OpenSteamTool`) + `opensteamtool.toml`; `Steam\cloud_redirect.dll`; `MugiLauncher.exe` (Qt6 client). Headers: `X-Requested-With: manilua-plugin`, `x-online-key`, `x-device-fingerprint` (HWID `mugi_xxxxxxxxxxxxxxx`), Bearer tokens.

**Key API facts**:
- `GET /api/plugin/health-manifest` -> `{requiredPluginVersion, files:[{fileKey, filename, sha256, downloadUrl, packageFiles[], updatedAt}], enforcePluginChecksum, maintenanceMode}`. Their best ops idea — checksum/version authority that force-gates client updates. **We copy this pattern (F-2)**, strictly better: one source of truth for app + plugin, fixing their own installer-vs-manifest hash drift.
- `/api/plugin-page`, `/api/plugin/access`, `/api/plugin/api-key` + `/bindings`, `/api/plugin/cloud` + `/devices`, `/api/plugin/discord/check`, `/api/plugin/join-clan`|`leave-clan`. Installer: `install.ps1`+`millennium.ps1`+`/download/{plugin.zip, ost.zip, cloud_redirect.dll, opensteamtool.toml, MugiLauncher.exe}`.
- Free tier = Discord guild hostage (leaving deletes key; nitro/mtag hostage tiers). Weekly quotas (manual/online/token) + daily usage; clan +10 weekly points; HWID binding + 24 h reset cooldown; guest checkout; Epic lifetime $49.99 / unlimited Denuvo tokens / 5 PCs.
- **MugiCloud** = per-key (not per-PC): `usedBytes/quotaBytes/baseBytes/bonusBytes/bonusExpiresAt`; 500 MB free/key; +1 GB/yr $9.99; buy-again-extends. Sits on Cloudflare R2 resold ~667x.

**Deep backend RE (rev 7, `plugin.zip` + SPA in-memory — full dossier in `infos.md` §1):**
- **Core constants:** `API_BASE_URL=https://mugi.store/api/plugin`, `ONLINE_API_BASE_URL=https://mugi.store`, update `/download/plugin.zip`, `${ONLINE_URL}/api/health` ping before worker jobs; `MUGI_LOCAL_BACKEND=1` → `127.0.0.1:4000` (dev override also auto-detects a local `my-react-app`); prefixes `mtag_|mugi_|premium_|epic_|nitro_`; RAR pw `online-fix.me`; timeout 8 / backoff 20.
- **`server_route` host-pin:** `Steam\config\manilua\api_server.txt` pins `mugi.store`|`mugiplugin.com`; `apply()` rewrites every base URL; `sync()` before each call. **Whole backend host-redirectable via one file** — EEG weak point; GabLuchi must pin its own endpoints at build time, never in a runtime-editable file. **Rev 8 (independent confirmation):** Mugi's own unreleased plugin changelog v1.2.0 advertises exactly this as a UX feature — "automatically switch to mugiplugin.com when mugi.store cannot connect, remember the selected server, choose either server in Settings" — two live hosts + remembered user default. Also corroborates the MugiPlay social feed (animated avatar frames gold/red in friends/requests/profile/discovery) and the health-repair flow (one-click repair-all + Steam restart).
- **Header set (all computed, none truly hardware-bound):** random `X-Steam-PID` 2000–32000; `X-Millennium-Version: 3.1.0`; `X-Plugin-Checksum` fixed hex; `X-Device-Fingerprint = djb2+FNV1a blend hex(COMPUTERNAME.."-"..USERNAME)`, 8 groups; `X-Process-Hash`/`X-Memory-Proof` = hex of `timestamp-process-data-<pid>`/`timestamp-memory-proof-<pid>`; UA `manilua-plugin/3.1.0 (Millennium)`. **F-7 must be strictly stronger (signed attestation, not hash-of-hostname).**
- **Key lifecycle:** persistent `config\manilua\{online_key.txt,api_key.txt}` (survives plugin updates; legacy volatile copies auto-migrate); `verify_online_key` → `POST plugin/online-files/authenticate`; **`newKey` rotation + `fallbackKey` silent swap**; wipes on banned/hwid_mismatch/device_blocked; daily-limit codes do NOT kill key. F-7 to mirror rotation.
- **Download engines = one generated PowerShell worker** (`spawn_background_worker`; `worker_<appid>.ps1` + `config_<appid>.json` + `status_<appid>.json`; types `manilua|online|d-games|game-fix`): unified `GET api/plugin/game/{appid}?dtype=1|2` (Lua-only vs Manifest — manifests go to **Steam-root `depotcache` active** + `config\depotcache` backup); online-fix `.rar` via server-served `unrar.exe x -p"online-fix.me"` or Expand-Archive flatten+merge; D-Games `POST d-games/{appid}/request` token-zip with `temp_token_<nonce>` staging; game-fix `GET plugin/game-fix/{appid}/download` + `fix_nonce`. Massive-online via `GET plugin/massive-online/{appid}`.
- **Manifest restore guard:** hidden PS1 + `FileSystemWatcher` on root depotcache (mutex `MugiManifestRestoreGuard`, filter `*.manifest*`), restore-from-backup on delete, 5 s rescan loop. Direct model for F-8's integrity story.
- **Health pipeline (pattern worth mirroring for F-2):** `health_manifest` GET **unauthenticated** → `{requiredPluginVersion, files[] sha256+downloadUrl}`; `UpdateHealthFile` stages → **SHA-256 verify** → detached replacer **kills `steam.exe`+`steamwebhelper`, copies, restarts Steam**; detachment via VBS/WScript **or a ONE-SHOT ScheduledTask `MugiHealthUpdate-<ts>-<rnd>`** (Limited run-level, self-unregisters, self-deleting PS1) → survives Steam being killed without needing admin. OST bundle = `OpenSteamTool.dll+dwmapi.dll+xinput1_4.dll`, delivered from `/download/ost.zip`.
- **MugiPlay record hook (exact):** webkit.js adds a MUGIPLAY tab (rewrites COMMUNITY text), and **after a successful `addViamanilua`, `if (enable_mugiplay) RecordMugiPlayGame({appid,gameName})`** → `POST mugiplay/activity`. Dashboard Epic bindings expose the per-device `mugiPlayEnabled` switch; plugin tab = consumption surface. Social RPCs: `GET mugiplay`, `POST mugiplay/friends/request`, `POST mugiplay/friends/{id}/(accept|decline|remove)`.
- **EEG weak points (for red-team/README):** forgeable fingerprint family; plaintext key files on disk; unauthenticated `health-manifest` (anyone can enumerate required DLLs/sha256 + download URLs → supply-chain depth signals); host-pin redirect + `MUGI_LOCAL_BACKEND` dev override = trivial backend swap; `.before-key-menu` `.bak` shipped inside the distro (repo hygiene).

**Pin-authority truth (OST/BetterSteamTools)**: `.acf` is state, never authority. A pinned build = (a) `depotcache/<depot>_<gid>.manifest` (protobuf magics `0x71F617D0`/`0x1F4812BE`/`0x32C415AB`), (b) Lua `setManifestid(depot, gid, size)` pin (Active vs Commented), (c) for injected clients, hook-time `BuildDepotDependency` patching `DepotEntry.{DepotId, AppId, ManifestGid, ManifestSize}`. Fix/Denuvo slots `forceLocked`. Depot keys: OST intercepts `LoadDepotDecryptionKey` at hook time (no key file on disk); file path (LuaTools) writes `depotID;hexKey` temp -> `DepotDownloaderMod ... -manifestfile` with anonymous `steamUser.LogOnAnonymous()`.

**MugiPlay (rev 7 FINAL correction — supersedes the rev 6 "audit toggle only" ruling which was itself wrong):** it is BOTH a per-device recording switch on Epic key bindings ("Disable MugiPlay to stop recording new library additions from that device") **and** a social discovery layer in the Steam client (MUGIPLAY tab replacing COMMUNITY — Mugi friends, activity feed, one-click `addViamanilua` from a friend's library). Record hook in webkit.js: `enable_mugiplay`-gated `RecordMugiPlayGame` right after successful adds. Build F-11 = per-device toggle **plus** a privacy-opt-in feed, not either-or.

**Do-copy list** (folded in):
1. Public health-manifest + checksum enforcement + maintenanceMode + force-update (F-2).
2. Per-device audit toggle + MugiPlay social feed — the real MugiPlay = both surfaces (F-11).
3. Buy-again-extends storage UX — reuse semantics, own storage (F-10).
4. Transparent quota counters with visible reset times, if quotas are added at all.
5. SHA-256-verified self-healing updater w/ Steam kill-restart, pinned on a one-shot ScheduledTask (no admin headless update). Mirror in F-2/F-6.
6. Buy-privacy edge: our anti-tamper headers must NOT copy Mugi's forgeable hash-of-hostname family — F-7 signs, doesn't obfuscate.

**Do-NOT-copy list (brand risk)**:
1. Discord-membership-hostage free tier (key deletion on leave).
2. Destructive "reset" that wipes Steam root / force-kills Steam mid-session.
3. Unsigned single-domain binary distribution (their 1/100 trust = our marketing gap).
4. Per-key cloud hostage with "delete everything" default.
5. Discord OAuth as sole identity source.
6. HWID-only identity with free slot reuse + 24 h cooldown.
7. `server_route` host-pin / runtime-editable endpoint config + `MUGI_LOCAL_BACKEND` dev override reaching prod (backend swap surface).
8. Plaintext API keys in a config dir + unauthenticated `health-manifest` enumerating DLL names/sha256/download URLs.
9. Shipping `.bak`/dev artifacts inside the production distribution zip.

---

## 4. On-PC reverse-engineering pass (checklist)

1. Clone: `OpenSteam001/OpenSteamTool`, `OpenSteam001/steam-monitor` (pattern branch), `BetterSteamTools` (git.lua.tools), `madoiscool/LuaTools` (C#), `SCPLEGION/Millenium-steamtools-ryuu`, `ZiggyMar/SteamUnlock`, `ToxcGang/OpenLuaTools`, `ToxcGang/OpenSteamToolPlugin`, `swwayps/luatools-moon`, `XingDG/Luatools`, `SteamAutoCracks/Steam-API-Check-Bypass`; pull `mugi.store` plugin.zip + `install.ps1`.
2. Ghidra/IDA the loader DLLs (`OpenSteamTool.dll`, `dwmapi.dll`, `xinput1_4.dll`) and map:
   - Lua C binding export table (`luaopen_*` registry) — exact function names to mirror.
   - IPC namespaces (`Global_SteamtoolsIPC_Class` + modern equivalents).
   - netpacket / eMsg dispatch points for package + PICS + rich-presence injection.
3. Diff OpenSteamTool layout (`config/lua`) vs legacy SteamTools (`stplug-in`) for plugin-dir + compat story.
4. Document SteamDRM off-by-four AppTicket forge + `extract_tickets` flow (run on title-owning machine, dump AppTicket/ETicket hex).
5. Build Lua conformance test suite (one script per API function) — CI gate for F-3.
6. Capture the four manifest-provider response formats (§ infos) as fixtures for F-8.

---

## 5. Feature sets

Effort scales relative (S/M/L). Native work -> `gabluchi-unlocker` (C++); orchestration -> `GabLuchi.Services`.

### F-1 · Core unlocker layer — client-side DRM bypass (S).

- **AppTicket forging (no process injection):** reuse Steam's local ConfigStore ticket, forge requested AppId via SteamDRM "off-by-four" ticket parsing. SteamStub-only games need no explicit ticket.
- **Ticket credential store:** `setAppTicket(appid, hex)` / `setETicket(appid, hex)` -> Windows registry `HKCU\Software\Valve\Steam\Apps\{appid}` (`AppTicket`/`ETicket` REG_BINARY); explicit > Cache; Denuvo requires explicit ETicket (store). SteamID per-app via `HKCU\...\ActiveProcess` `ActiveUser`/`Universe` — fork inherits the exact keys Steam reads.
- **Steam session IPC spoof:** `Global_SteamtoolsIPC_Class`-style so Steam treats GabLuchi data as client data.
- **Injection primitives:** package injection; PICS access-token injection; depot decryption-key injection (**prefer hook-time `LoadDepotDecryptionKey` — no key file on disk**; keep `depotID;hexKey` temp + `DepotDownloaderMod` anonymous path as non-injected fallback); manifest pinning via KeyValues VDF patching (`SetManifest*`/`ManifestPins`) **+ `BuildDepotDependency` `ManifestGid` patch**; `FakeAppIds` (Spacewar 480) for online fixes.
- **Pin authority rule (from RE):** `.acf` is state, not control. Pins = depotcache manifest presence + Lua pin declaration + hook-time patch; `forceLocked` for managed fix/Denuvo slots.
- **Fake-license injection (from `Hooks_Package.cpp`):** `CheckAppOwnership` hook — if `HasDepot(appId,false)`: genuinely owned (`ExistInPackageNums>1`) → `MarkOwned`; else inject `PackageId=kInjectedPackageId(0)`, `ReleaseState=Released`, `bOwnsLicense=true`, `bFreeLicense=false`. `InitFakeLicenseOnce` pulls `GetPackageInfo(0, kInjectedPkgAccessToken=10660652434190618804ull)`, grows AppIdVec with every declared depot, fires `MarkLicenseAsChanged(0,true)` + `ProcessPendingLicenseUpdates`.
- **OnlineFix Spacewar 480 dance (from `Hooks_Misc.cpp`):** `SpawnProcess` int3-trap rewrites `pGameID→480` (Spacewar) while saving `g_OnlineFixRealAppId`; `OptedInMask` + overlay `CGameID` rewrite back to real; `IClientUtils::GetAppID` post-spoof restores real AppID. Fake AppIds (480) only for ownership/online-fix context, real ID everywhere else.
- **Code:** new `GabLuchi.Services/UnlockerCoreService.cs` bridging native `gabluchi-unlocker`; reuse `SteamAppInfoCache`, `ManifestDownloader`, `SteamDepotInfo`.
- **Done when:** a random SteamStub-only game launches via GabLuchi with no `.bak` swaps and no game-process injection.

### F-2 · Version-resilient pattern engine + health-manifest authority (S).

- SHA-256 `steamclient64.dll` + `steamui.dll` on every launch; pattern TOML from mirror chain GitHub raw -> CDN (jsDelivr) -> local cache; 404 -> fall through, disable only hooks tied to that DLL.
- Hot-reload watched config; unknown build -> one-shot popup (DLL name + hash + cache path).
- **Health-manifest endpoint (copy Mugi's ops design):** `GET /api/plugin/health-manifest` -> `{requiredVersion, files:[{fileKey, filename, sha256, downloadUrl, packageFiles[]}], enforceChecksum, maintenanceMode}`. Single source of truth for app + plugin; fixes their installer-vs-manifest drift.
- **Code:** `SteamService.cs` (fingerprint), new `PatternRegistry.cs` + mirror client (reuse `GithubProxy`), new `HealthManifestService.cs`, `SettingsService`.
- **Done when:** patched `steamclient64.dll` detected -> hooks adapt, other hooks still work; app + plugin health-checks agree on one manifest.

### F-3 · Full Lua runtime parity (M).

- Keep `LuaFileParser`/`LuaInstaller` for install/fix scripts.
- Add **runtime**: auto-load `.lua` from `<steamroot>/config/lua` + `stplug-in`; compile via `luapacka.exe`-compatible bytecode; function names case-insensitive.
- API: `addappid`, `setmanifestid`/`setManifestid`, `addtoken`, `setAppTicket`, `setETicket`, `pinApp`, `fetch_manifest_code_ex`. Preserve lines from downloaded scripts. **(OST v1 `LuaConfig.cpp` registers only `addappid, addtoken, setmanifestid, http_get, http_post, setappticket, seteticket, setstat`; `pinApp` is defined but not registered — F-3 should register our own `pinApp`, `fetch_manifest_code{,_ex}`, `setstat` for full catalog compat.)** Case-insensitive lookup via lowercase registration + `_G` metatable, matching OST.
- **Pin state machine:** Active / Commented; compare vs `api.steamcmd.net` public gid (already in `LuaFileParser`); `forceLocked` for managed fix slots.
- Conformance suite (§4) as CI gate.
- **Done when:** entire LuaTools/OpenLuaTools catalog runs unmodified.

### F-4 · Steam-side Rich Presence + achievements + wire replay (M). [RENAMED — real home is the netpacket layer]

- **Copied from OST `Hooks_NetPacket.cpp` (rev 5/6 finding)** — this is OST's largest hook module (16 handler namespaces) and the true home of RP/achievements/cloud/onlinefix/moving-wire spoofs, NOT the IPC layer:
  - Send (`BBuildAndAsyncSendFrame` hook) + Recv (`RecvPkt` hook) with pooled packet replacement + **two delivery tricks (rev 6):** in-place body resize (serialize into original buffer, fake `m_cubData`) and **carrier-borrow** (swap `m_pubData`→manufactured packet for exactly one `oRecvPkt` call — used for injected 766 persona + synth 147 cloud responses, queue ≤64).
  - eMsg 151 → service-job dispatch by FNV hash of `target_job_name`: `GetUserStats` (send+recv), `GetManifestRequestCode` (wire-level manifest-code replay → F-8), `Cloud.*` (cloud-save redirect over the wire via `CloudRedirectHost`, pass-through for `SignalAppExitSyncDone#1`/`ClientConflictResolution#1`).
  - `8903` PICS access-token injection; `742/5410` GamesPlayed (OnlineFix rewrite); `7501` RichPresenceUpload (track); `818` GetUserStats send; `5466` StoreUserStats2 (notify cloud stats stored); `766` ClientPersonaState (recv replace — **RP spoof**); `819` GetUserStatsResponse (recv replace — **achievement stats spoof**); `1183/9406` FamilySharing null-body suppression.
  - IPC layer handles only: `GetSteamID` + ticket routes + `GetAppID` + `GetAPICallResult` (see §10). **F-4 must port the netpacket module, not build fresh.**
- Legacy "MugiPlay social (Discord)" ruling superseded (rev 7): MugiPlay IS social (MUGIPLAY feed tab, friends, one-click adds) **plus** per-device recording. F-11 = both surfaces; nothing dropped.
- **Code:** port `Hooks_NetPacket.cpp` handlers + `steam_messages.pb.h` protobuf build; `LobbyBrowserService`, `ConnectRelayService`, `HttpServerService` extension, `HubcapStats` (rename/absorb).
- **Done when:** a gateway shows correct playtime + achievements row for an unowned game.

### F-5 · Linux / Steam Deck support (L, stretch).

- Port loader strategy: `LD_AUDIT`/`LD_PRELOAD` wrapper + Lumen-style Lua sidecar + Millennium-compatible plugin frontend.
- CloudRedirect `.so` (Windows exists); `WINEDLLOVERRIDES` + Proton launch options per fix.
- **Done when:** `gabluchi-plugin` + unlocker run on unmodified Steam Deck.

### F-6 · Integrity hardening (M) — protects users and brand; antidote to Mugi's 1/100 trust.

- **Hash-pin loader DLLs** against health-manifest before install.
- **Atomic staging** (download -> tmp -> validate -> copy; OpenLuaTools "ask, verify, restart Steam" pattern).
- **Network-endpoint scan** on downloaded Lua/fix archives before install.
- **No remote loaders, ever** — no `HttpLoadDLL`-style fetch-and-exec.
- **Signed distribution**: real release signing + pinned hashes + clean update channel (what Mugi lacks and gets blacklisted for).
- **Tiered logging** (main/ipc/netpacket/manifest/decryptionkey).
- **Done when:** seeded malicious fix archive rejected at install with user-visible quarantine.

### F-7 · License Gate DLL (L) — locked spec, owner decisions recorded.

**Owner decisions (2026-09-19):** hard block when app is off; all app-touched games incl. online fixes; trust = issued license keys.

- **Trust (license-key anchored):** runtime validates license + HWID binding (`HardwareAppIdService`) + appid entitlement, mints fresh **Ed25519-signed ticket** per launch (private key agent-side, public key baked in shim). Ticket = appid + HWID + license id + short TTL. Shim verifies locally -> game runs.
- **Hard block:** no last-good, no grace. App off -> no ticket -> refuse. Expired/revoked -> refuse mint -> immediate block. Shims hold no memory; agent is sole mint. Startup-race retry ~10 s (race tolerance, not grace).
- **Coverage:** outermost proxy DLL (`version.dll`/`winmm.dll`/`steam_api64.dll` name, exports forwarded to `.bak`); bypass/fix shims chain inward. Reuse `SteamApiCheckBypass` file-redirect logic in `gabluchi-unlocker`.
- **Online-title detail:** anti-cheat-sensitive titles gate once at process init then pass control to the fix wire; per-title `skip-in-game-check` flag in `FixRepository` manifests.
- **Anti-circumvention:** staggered re-checks + boot check; self-CRC + watchdog; monotonic-clock anti-rollback; obfuscated CF + string encryption (LTCG/whole-program); shim build hash via `AnalyticsService` -> patched forks flagged -> ban-wave = stop minting.
- **Code:** `/license/verify`, `/license/status`, `/license/revoke` in `HttpServerService.cs`; `LicenseService` mint + expiry; `UnlockerService`/`PluginInstallerService` drop-in + `.bak`; `DefenderService` exclusion; AV-friendly signatures.
- **Done when:** removing key (or closing app) makes a fresh offline game AND an online-fixed title refuse to launch; valid keys launch clean.
- **Note:** kills the old "no license system / tool is free" non-goal — `AuthService`/`LicenseService`/`api.json` exist and F-7 makes keys core.

### F-8 · Multi-provider manifest engine (M). [NEW]

- First-class providers + parsers: opensteamtool (`manifest.opensteamtool.com/{gid}`, plain uint64), wudrm (`gmrc.wudrm.com/manifest/{gid}`, plain digit), steamrun (`manifest.steam.run/api/manifest/{gid}` -> `{"content":"..."}`), ryuu generator (`generator.ryuu.lol`, X-Auth-Key, `file_type=lua|manifest|zip`, `branch`), hubcap (existing paid), perondepot-style mirrors, GitHub mirror, `gabluchi-manifests`. SCPLEGION-style fallback backend (`/exists`, `/sign?ttl=`, `/file/{appid}`) as a provider shape. (Route maps in `infos.md §11`.)
- **Provider truth (from OST `ManifestClient.cpp` + live probe):** the classic trio resolves **GID → request code** (not GID → manifest); `opensteamtool` endpoint serves plain uint or HTML (Cloudflare-ish), wudrm = plain digit, steamrun = JSON; **wudrm ↔ steamrun are one mirrored backend** (identical codes, both directions). So the "3 providers" is really 1–2 legs — F-8 must add genuinely independent legs and treat the classic trio as fall-back one leg.
- **Wire-level replay path (rev 5):** OST also re-serves manifest codes inside the client via netpacket handler `Hooks_NetPacket_Manifest` (`GetManifestRequestCode` service job, send+recv) — requests never leave the machine when an override exists. F-8 should offer the same "local override beats provider" fast path: check local `ManifestOverride` map + Lua `fetch_manifest_code{,_ex}` before fanning out to providers.
- **Race-and-fallback chain:** parallel fan-out per `(depot_id, gid)`, fastest healthy wins, fall through on 404/429/timeout with per-provider backoff; TTL cache; per-provider health in Settings.
- Kill unstable raw-IP defaults (`167.235.229.108`) -> named providers + domain/TLS.
- **Code:** new `ManifestProviderRegistry.cs` + providers; adapt `ManifestDownloader`/`ManifestPreCacheService`; `api.json` stays as user override.
- **Done when:** simulate dead mirror -> engine lands manifest via another provider, logs which won.

### F-9 · Version selector (M). [NEW — completes existing 80% infra]

- `ManifestHistoryService`: per-depot history via steamcmd.net `depots`/`branches` public build IDs + local history cache + custom manifest-ID input.
- RPCs on 6767: `GET /manifests/{appid}`, `POST /manifests/{appid}/{manifestId}`, `GET /version-status/{appid}`.
- UI: version dropdown in game detail + "always up to date" toggle.
- Pin mechanics: toggle = active vs commented Lua pin; download manifest via F-8 into `depotcache`; `forceLocked` for managed fix paths.
- **Code:** `ManifestHistoryService.cs`, RPCs, `DepotDownloaderModService` reuse, `ManifestPreCacheService` ext, `AppDetailViewModel`/plugin JS.
- **Done when:** install older build from dropdown, Steam stays pinned, flipping toggle unpins to public.

### F-10 · Cloud saves (R2) (M). [NEW — FRONT-LOADED by owner]

- **Storage (owner choice: per-device, NOT Mugi's per-key model):** S3-compatible Cloudflare R2 via `AWSSDK.S3`; key layout `saves/{appid}/{deviceId}/{timestamp}/`; 3 backups; conflict = latest timestamp wins; save-dir from `appmanifest_*.acf` `userglob` + `GamePortLookup` ext.
- **Wire redirect exists upstream (rev 5):** OST's netpacket layer already answers `Cloud.*` service jobs locally via `CloudRedirectHost` (suppresses the outbound frame; synthesized response from `RecvPkt` hook), with `Cloud.SignalAppExitSyncDone#1` / `Cloud.ClientConflictResolution#1` passed through untouched. F-10 should mirror this: cloud-save read/write serviced in-process, don't rely on Steam's server path alone.
- **Mugi UX copied (good parts):** per-device on/off toggle (F-11 plumbing), storage meter (`usedBytes/quotaBytes`), buy-again-extends semantics, and **keep-my-saves / delete choice that defaults to KEEP** (Mugi's "delete everything" is a don't-copy).
- RPCs: `GET /cloud/{appid}/status`, `POST /cloud/{appid}/upload|download|toggle`, `GET /cloud/devices`.
- **Code:** `CloudSaveService.cs` (R2), `CloudRedirectService` reuse, `HttpServerService` RPCs, `SettingsService`, `ManageViewModel`.
- **Done when:** two devices sync same game's saves via R2 with 3-backup + latest-wins; a toggle turns a device off.

### F-11 · Device recording toggle + MugiPlay feed (S). [NEW — MugiPlay = BOTH surfaces, rev 7]

- Per-HWID-binding recording toggle (Mugi's `mugiPlayEnabled` copy): on = record library-add/fix events to backend; off = zero events from that device.
- **Social layer (the second MugiPlay surface, confirmed in `webkit.js`):** optional friends-by-ID (`MUGI-ABCD-2345`), activity feed of friends' added games, one-click add via `RecordMugiPlayGame` hook → own `addViamanilua` equivalent; fronted as a MUGIPLAY tab in the plugin, exactly like Mugi's COMMUNITY→MUGIPLAY rewrite.
- Rollout default: opt-in at onboarding, recording off + feed off by default (stronger privacy positioning vs Mugi).
- **Code:** `AuthService`/`ApiClient` event-reporting flag, `HardwareAppIdService` binding, `SettingsViewModel`, backend store, friendId/canary UX.
- **Done when:** device with toggle off records zero events and has no feed presence; toggled-on friend-feed shows another user's adds with one-click library add working.

---

## 6. Sequencing (rev 2 — owner front-loaded F-10)

1. **F-10 · Cloud saves (R2)** — owner front-load.
2. **F-2 · Patterns + health-manifest authority** — de-risks all native hooks; feeds F-6/F-7.
3. **F-8 · Multi-provider manifest engine** — resilience; bugfixes from §7 ride along.
4. **F-9 · Version selector** — differentiator on top of F-8/F-2.
5. **F-11 · Device audit toggle** — small; bundles with F-10 backend.
6. **F-6 · Integrity hardening** — before F-7 ships a gated DLL.
7. **F-1 · Core unlocker layer** (needs F-2 patterns).
8. **F-3 · Lua runtime parity** (on F-1).
9. **F-4 · Rich presence / achievements / wire replay** (on F-1).
10. **F-7 · License gate** (needs F-1 + F-6).
11. **F-5 · Linux/Deck** (stretch).

Owner roadmap interleave: health-check UI polish (logic exists); MugiPlay = per-device recording toggle + friends feed -> F-11 (now covers both surfaces, rev 7).

---

## 7. Bug & hygiene list (fix before feature branches)

**Code bugs:**
- `MultiplayerFixService`/`SteamRipService`: buzzheavier-host IDs routed to pixeldrain API URL (wrong API). Native buzzheavier client or guard.
- `OnlineFixEntry.SizeBytes` always 0 — size regex result never stored.
- `GitHubFixService` index cache TTL 5 min vs 24 h elsewhere — normalize.
- Duplicate apply pipeline across `OnlineFixService`/`GitHubFixService`/`CrackFixService` (`ResolveGameDir`/`IsFixFile`/`FixOnlineFixIni`) — factor into shared `FixApplier`.
- `ManifestDownloader` `api.json` default raw IP `167.235.229.108` — see F-8.
- 7za located via `AppDomain.BaseDirectory`, docs say `third_party/` — align.

**Repo hygiene:**
- `online.md` / `multiplayer-storage.md` / `Mugi_v5.0.0_Feature_Breakdown.md` stale vs shipped code (Health Check IS built; MugiPlay = recording toggle **AND** social feed — rev 7). Update or delete.
- Version skew: dissected MugiLauncher is v2.3.0, docs target v5.x — tag RE artifacts with real version.
- Committed build artifacts: `CommunityToolkit.Mvvm...__Internals/`, `System.Text.RegularExpressions.Generated/`, `GabLuchi_1qmj4x4m_wpftmp.csproj`, `obj/`. Clean + gitignore.
- Committed RE artifacts: `Reverseengineer/MugiLauncher_2.3.0_win_x64.zip` (+ extracted exe), `relay.exe~`. Decide policy.
- `fixes_multi/` RARs duplicated into gabluchi-fixes — single source of truth.

---

## 8. Decision log

| Date | Decision |
|---|---|
| 2026-09-19 | F-7 gate: hard block when app off; covers all app-touched games incl. online fixes; trust anchored to issued license keys. |
| 2026-09-19 | Mugi pass: front-load **F-10 cloud saves**. Copy **health-manifest pattern** (F-2) + **per-device audit toggle** (F-11). Do NOT copy per-key cloud model, Discord-hostage tiers, destructive reset, opaque quotas, unsigned single-domain distro. |
| 2026-09-19 | ~~Drop "MugiPlay social (Discord)" — MugiPlay is an audit toggle. Build F-11 instead.~~ **SUPERSEDED 2026-09-20:** rev 7 confirmed MugiPlay = recording toggle + social feed. See new rows. |
| 2026-09-19 | Old "no license system / free tool" non-goal reversed — `AuthService`/`LicenseService` exist; F-7 makes keys core. |
| 2026-09-19 | F-1 approach = real fork of OpenSteamTool core into `gabluchi-unlocker` (rebrand + harden + own health-manifest). NOT clean-room, NOT bundled-OST-binary. |
| 2026-09-19 | Backend = reuse existing GabLuchi stack (§11). No new web backend. |
| 2026-09-19 | Denuvo eticket pool-mint DEAD — owner doesn't own games; etickets prove ownership. Strict-Denuvo handled by scene (proper cracks / HVB metadata), never in-app minting. |
| 2026-09-19 | Denuvo surface = metadata + informed-choice only. DenuvOwO (HVB) is a manual high-risk workflow — never auto-apply; guide at `DENUVOWO_GUIDE.md`. |
| 2026-09-20 | Deep RE fold (rev 7): Mugi is **v5.2.0**; MugiPlay = per-device recording toggle **+** social feed (both surfaces — supersedes the 2026-09-19 "audit toggle only, drop social" row). F-11 now builds both. |
| 2026-09-20 | Mirror Mugi's ops patterns in F-2/F-6: SHA-256-verified self-healing DLL updater with Steam kill-restart; one-shot ScheduledTask persistence (no admin headless update); depotcache restore guard (F-8). |
| 2026-09-20 | Do NOT copy: forgeable header family (F-7 signs instead), `server_route` host-pin/`MUGI_LOCAL_BACKEND` backend-swap surface, plaintext key files, unauthenticated health-manifest, `.bak` artifacts in distro. |
| 2026-09-20 | Mugi RE master dossier folded into `infos.md` §1 (full RPC surface from `main.lua`, key rotation lifecycle, worker engine, manifest guard, health pipeline, SPA routes); `infos.md` header bumped to rev 7. |
| 2026-09-20 | Rev 8: Mugi's own unreleased plugin changelog v1.2.0 independently confirms the RE — dual-server failover (mugi.store↔mugiplugin.com, remembered + Settings selectable), MugiPlay social feed (animated avatar frames), one-click health repair + Steam restart, `requiredPluginVersion` (>5.1.2 detection), Lua-only downloads. Folded into `infos.md` §1 + PLAN §3. |
| 2026-09-20 | Rev 9: voices38 (proper-crack road) sued by Denuvo 2026-09-14 (§1201, N.D. Cal, 26 games) — he says work continues. Listings keep `voices38 (crack)` + `DenuvOwO` tags as the two-roads model; supply-watch voices38 (lawsuit fallout → HVB demand spike); never bundle either. Scene-status note added to `DENUVOWO_GUIDE.md` + `infos.md`. |
| 2026-09-20 | Rev 10: GabLuchi "Fix applied / DONE but game won't launch" root-caused in source + live probes — the Fix button downloads the **manifest** (`fallbackName` from `fix.FixFilename` is dead code; both slots hit `DownloadManifestAsync(appid,"Ryuu")`); **no fix route exists anywhere** (worker `/fix/` =404, `lua.tools/api/denuvo/fix` =404, `167.235.229.108/*.zip` =400; `DenuvoDownloadResponse` unused). Fix spec in `PATCH.md`: token-gated worker `/fix/{appid}` → `{url}`, park scraped DenuvOwO/voices38 zips in `gabluchi-fixes/fixes/denuvo/` + `denuvo.json` (`cs.rin.ru`; keep `online-fix.me` for the rar pool), client slot rewiring + 7za extraction + `.bak` + tag-aware post-apply + buzzheavier misroute fix. Informed-choice gate for DenuvOwO stays mandatory. |
| 2026-09-20 | Rev 11: DenuvOwO token/expiry model locked via RD945/hypervisor-crack-audit (RE Requiem HYPERVISOR.V2-KIRIGIRI, 2026-03-04, static-only, 41,644 lines Ghidra pseudocode, no malware evidence) + Habr/CRACKLAB (Blaukovitch = Rose/Natasha 0x80000003, Denuvo-approved). Exact facts now in docs: fake-license cert window `260226065959Z→270226071959Z` (**expires 2027-02-26**) = the old "~Feb 2027"; Denuvo license read ID `92346205896` → `CreateFileW` IAT redirect → `KIRIGIRI.bin` (runtime-written, CPU-vendor-specific); built-in UEFI-variable DSE bypass magic `0xDEADC0DE` (EfiGuard optional, VBS.cmd F7 one-shot = V3 convenience); launch base detanup01 GBE fork + HyperEvade; KIRIGIRI.dll 9-step entry (PEB spoof, syscall numbers, module cloaking, watchdog.exe, CPUID regs `0x69696969`/`0x336933`/`0x1337`); two generations V2 ColdClientLoader vs V3 Steam+VBS.cmd; per-fix expected-file validation must NOT require runtime-created `KIRIGIRI.bin`. Folded into PLAN §11/§12, `infos.md`, `DENUVOWO_GUIDE.md`, `PATCH.md` appendix. |
| 2026-09-20 | Rev 11.2: Rev 10's "no fix route exists anywhere" is **wrong** — lua.tools HAS a session-gated download API (`/api/denuvo/download?fix={uuid}&slot=fix`, Supabase session → pre-signed Cloudflare R2 URL, 120 s, `filename={appid}.zip`; confirmed via browser HAR capture). Fix is client-only: `GetDenuvoDownloadAsync` + `ILuaToolsSession`/`RefreshTokenSession` (shared account), rewire `RunDownload` fix slot (repo-cache → API → stream → 7za → ApplyToGame), delete dead `fallbackName`. Worker `/fix` + park-zips design **demoted to fallback** (`PATCH.md §5.4`). Folded into `infos.md §12` + `PATCH.md §4/§5`. |

---

## 9. Source references

- OpenSteam001/OpenSteamTool + OpenSteam001/steam-monitor (`pattern`/`ipc`/`protobuf` branches) — patterns, Lua, IPC, tickets, netpacket replay, `LoadDepotDecryptionKey`/`BuildDepotDependency`, DenuvoAuth/protection-scan module (PRs #109/#121/#128/#140).
- BetterSteamTools (git.lua.tools) — depotcache format, pin state machine, filename cipher.
- madoiscool/LuaTools (C#) — ManifestFile/DepotCacheMigration/DepotDownloaderService/BuildsViewModel mirror.
- SCPLEGION/Millenium-steamtools-ryuu — Lua backend `/exists` `/sign?ttl=` `/file/{appid}` ticket/download pattern (route map: `infos.md §11`).
- ZiggyMar/SteamUnlock — 17-provider + `depotcache/**/*.vdf` key dump.
- ToxcGang/OpenLuaTools + OpenSteamToolPlugin — Millennium plugin layer + stplug-in compat.
- swwayps/luatools-moon (+ deepwiki docs) — Linux stack, CloudRedirect, fix overlays, perondepot parsing.
- Rattpak/CEG-Anti-Tamper-Analysis — CEG tamper research (before shims on protected titles).
- SteamAutoCracks/Steam-API-Check-Bypass — CreateFile redirect/hide + nth-time hooks.
- Hegxib/SteamTools-Deep-Analyze — trojanized SteamTools backdoor write-up (F-6 threat model).
- mugi.store — live installer, `install.ps1`, `/api/plugin/health-manifest`, SPA bundle; owner's `Reverseengineer/MugiLauncher_2.3.0_win_x64.zip` + `mugi.md`. Full raw findings in `infos.md`.

---

## 10. OpenSteamTool source-level anatomy (rev 5/6 RE rounds — source fetches)

Empirical, from reading OST source files directly (recorded in `infos.md §7`). This makes F-1/F-2 pin down to the file level.

- **Off-by-four AppTicket forge** (`src/Utils/Tickets/AppTicket.cpp`): source ticket = cached local ConfigStore ticket for `kLocalAppTicketSourceAppId = 7`. Exact math, from source:
  - Header constants (`AppTicket.h`): `kAppTicketSteamIdOffset = 8`, `kAppTicketAppIdOffset = 16`, `kAppTicketSignatureSize = 128`; `AppTicketSource {CredentialStoreOnly, ForgeOnly, CredentialStoreThenForge}`.
  - Forge (`ForgeLocalAppOwnershipTicket`): `signedSize = source.size() − 128`; copy signed body, append **4 forged AppId bytes**, append 128-byte signature → physical size = `source.size() + 4`. Reported `totalSize = physical − 4 = source.size()`; `appIdOffset = totalSize − 128`; `signatureOffset = appIdOffset + 4`; `steamIdOffset = 8`; `signatureSize = 128`. **The +4 hurls the forged AppId into SteamDRM's off-by-four parse window.**
  - Credstore path (`GetAppOwnershipTicketFromCredentialStore` + `GetAppOwnershipTicket`): returns ticket with `signatureOffset = *reinterpret_cast<const u32*>(ticket.data())` (first u32 is the real signature offset); gates on `LuaConfig::HasDepot(appId)` (must be declared via `addappid`).
  - SteamID resolution: `GetSteamIDFromCredentialStore(appId)` per-app; `kSteamIdTicketMinimumSize = 16`.
- **Depot-key hook** (`src/Hook/Hooks_Decryption.cpp`): hooks `ConfigStoreGetBinary` (`k_EConfigStoreUserLocal`), matches `...\<DepotId>\DecryptionKey`, `memcpy` key from LuaConfig store; `ReadConfigStoreLocalBinary` reads `apptickets\{appId}`. Confirms hook-time key injection (no key file on disk).
- **Manifest pin** (`src/Hook/Hooks_Manifest.cpp`): `BuildDepotDependency` hook patches `DepotEntry.{ManifestGid, ManifestSize}` in the returned vector; size=0 → keep original.
- **LuaConfig C++ surface** (`src/Utils/Config/LuaConfig.h`): `HasDepot, IsOwned, MarkOwned, GetAllDepotIds, GetDecryptionKey, GetAccessToken, GetStatSteamId, pinApp, GetPurchaseTime, GetManifestOverrides(ManifestOverride{gid,size})`, `ParseFile/UnloadFile/ParseDirectory/ReloadDirectories`, `TakePendingRemovals|Additions`, `HasManifestCodeFunc/CallManifestFetchCode{,Ex}` → **the whole F-9 pin surface is these calls**.
- **Lua → C++ registered surface (v1, exact)** (`src/Utils/Config/LuaConfig.cpp`): registered funcs are exactly `addappid, addtoken, setmanifestid, http_get, http_post, setappticket, seteticket, setstat` — case-insensitive (`register_func` writes lowercase + `_G` metatable `__index`). **`pinApp` is DEFINED but NOT registered** (`// register_func(g_lua_state, "pinapp", lua_pinApp);`). Semantics: `addtoken(appid, "decimal-u64")`; `setmanifestid(depot, "gid-string")` forces size 0 (per-file overrides stack then `RebuildManifestOverride`); `setAppticket/setETicket(appid, hex)` → hex→binary → credential store; `setStat(appid, "steamid-u64")`. F-3 parity must mirror THIS set, not the legacy SteamMidra marketing list.
- **Credential persistence layer** (`src/OSTPlatform/Windows/SteamCredentialStore.cpp`): plain Windows registry.
  - `HKCU\Software\Valve\Steam\Apps\{appid}` → `SteamID` (REG_SZ), `AppTicket` (REG_BINARY), `ETicket` (REG_BINARY) — **the same keys Steam reads**; fork inherits the layout for free.
  - `HKCU\Software\Valve\Steam\ActiveProcess` → `ActiveUser` (accountid), `Universe` (string) — `GetActiveUser()` feeds DenuvoAuth universe selection (Public/Beta/Internal/Dev).
- **TOML schema** (`opensteamtool.example.toml`): `[log]`; `[manifest] url = opensteamtool|wudrm|steamrun` + HTTP timeouts; `[stats] enable_api`; `[lua] paths`; `[inject] enabled/library_x64/_x86`; `[cloud] enabled/library`; `[remote] url_template = {channel}/{component}/{sha256}` (jsDelivr `OpenSteam001/steam-monitor@{channel}/{component}/{sha256}.toml`). The URL values are not endpoints — they select a named provider row.
- **Manifest provider engine** (`src/Utils/SteamMetadata/ManifestClient.{h,cpp}`): providers map **manifest GID → request code**, not GID → manifest. `kProviders[]` table:
  - `opensteamtool` → `https://manifest.opensteamtool.com/%llu` (default; parses plain uint64).
  - `wudrm` → `http://gmrc.wudrm.com/manifest/%llu` (plain-digit body).
  - `steamrun` → `https://manifest.steam.run/api/manifest/%llu` (`{"content":"<uint64>"}` JSON).
  - Priority per `FetchManifestRequestCode` (_lock-guard serialized, WinHTTP): Lua `fetch_manifest_code_ex(appId, depotId, gid)` → Lua `fetch_manifest_code(gid)` → active provider. Lua returns a *digit string* to dodge double-precision loss >2^53. Adding a provider = one row in `kProviders[]`.
  - **Live probe today:** wudrm (HTTP) and steamrun (HTTPS) returned **identical codes** for identical input (`17681624982582985530`, then `7567320866597989171` on re-resolution) → mirrored/shared backend. Three providers advertised, two real backends — resilience datum for F-8 (provider diversity is illusory; mirror chain must include truly independent legs like hubcap + ryuu + own).
- **Dual-channel pattern fetch** (`src/Utils/SteamMetadata/RemoteToml.h`): `Request{channel: pattern|ipc, component: steamclient|steamui, dllPath}` → `Result{ok, fromCache, body, sha256}`; remote TOML then local cache. `PatternLoader.h`: load metadata **before hooks**, RVA-first then signature scan, `ReportMissingFunctions`.
- **IPC schema** (`src/Steam/IPCMessages.steamd`): cmds `InterfaceCall=1, FlushCallbacks=2, Destroy=5, Heartbeat=6, Handshake=9`; results `OK=0x0B/Failed=0x0C`; interfaces `IClientUser=1, Friends=3, Utils=4, Apps=8, UserStats=11, AppManager=17, ConfigStore=18, GameCoordinator=19, UnifiedMessages=25`; **funcHash-coded dispatch** → F-2 pattern TOML position-independence.
- **Log tiers** (`src/Utils/Logging/LogModules.def`): `ipc, netpacket, manifest, keyvalue, decryptionkey, misc, achievement, pics, onlinefix, richpresence, package, steamui, pipe, platform` — directly maps F-6 tiered logging.
- **SteamMidra 16 Lua bindings** (F-3 parity *reference list, not authoritative*): `addappid, addtoken, setmanifestid, setappticket, seteticket, setstat, lchttpget, lchttppost, fetchmanifestcode, fetchmanifestcodeex, getcachedappticket, getdecryptionkey, seteticketurl, forcedenuvo, skipmanifestpin, addprocess`. Registry forgery `HKCU\Software\Valve\Steam\Apps\{AppId}\AppTicket|ETicket` REG_BINARY; `steam.cfg ForBootStrapperInAll=Enable`; 42+128 ticket. **v1 OST `LuaConfig.cpp` registers only 8 of these — see the registered-surface bullet above; treat SteamMidra as legacy/upstream-name compat, not the F-3 contract.**
- **Denuvo auth (replaces the PR #148 ghost):** no Web/eticket-url pipe. Real mechanism = in-process pipe auth (`src/Pipe/Features/DenuvoAuth/DenuvoAuth.cpp` + `ProtectionScan.{cpp,h}`, PR series **#109/#121/#128/#140** — #148 does NOT exist):
  - `ScanProtection(process.pid).denuvoDetected` gates the pipeline. Legacy scan = 5 sections (`.arch .srdata .xpdata .xdata .xtls`) + `DENUVO` string; high-version fast path scans only the OEP section for `DODENUVO` bytes `48 B9 44 4F 44 45 4E 55 56 4F`.
  - Pipe stages `None/Authorizing/EndAuthorization`, `kEndDenuvoVerificationHandshake = 2`, per-pipe authorization window (`IsAuthorizedPipe`/`CanUseAuthorizedIdentity`), universe from `SteamCredentialStore::GetActiveUser(accountId, universeName)`.
  - PR #121: always scan the main executable, not only modules ≥80 MB. PR #128: fix ticket SteamID offset (Token Error) in `AppTicket.cpp`. PR #140: engage auth path when signature scan misses but an ETicket is injected.
- **`tools/extract_tickets` — ticket-harvest CLI design** (reuse for F-1's data-gathering tool): x64-only; reads `HKCU\Software\Valve\Steam → SteamPath`, loads real `steamclient64.dll` (`SetDllDirectoryA` + `LOAD_WITH_ALTERED_SEARCH_PATH` so tier0/vstdlib resolve); `CreateInterface("SteamClient023")`; `CreateSteamPipe()` + `ConnectToGlobalUser()` against the running Steam; **sets `SteamAppId`/`SteamGameId` env BEFORE steamclient64.dll initializes** so `GetAppID` resolves to target (the runtime precedence trick F-1 relies on). Pulls ownership ticket synchronously via `STEAMAPPTICKET_INTERFACE_VERSION001 → GetAppOwnershipTicketData(appId, buf, 2048, &appIdOffset, &steamIdOffset, &signatureOffset, &signatureSize)` (works for any owned+cached app), and encrypted ticket async via `SteamUser023 → RequestEncryptedAppTicket(null,0)` + `SteamUtils010::IsAPICallCompleted`/`GetAPICallResult` (callback 100+54, 15 s bound). Writes `<appid>\appticket.bin`, `eticket.bin`, `tickets.txt`. Universe enum: `k_EUniversePublic=1, Beta=2, Internal=3, Dev=4`.
- **Ticket cache source (rev 5 correction):** forge/ticket reads do NOT hit the registry — `GetCacheAppOwnershipTicket(appId)` reads `apptickets\{appId}` from the **session ConfigStore** via the hooked `ConfigStoreGetBinary`. The `ConfigStoreGetBinary` hook also captures the local ConfigStore pointer once and implements the depot-key injection (`...\<DepotId>\DecryptionKey` → memcpy key, return key size as length). Registry (SteamCredentialStore) is the *write-target* for `setAppTicket/setETicket/setStat`, and the *priority source* only when `CredentialStoreThenForge` is selected.
- **Full hook inventory (rev 5, `src/Hook/*`):**
  - `Hooks_Decryption.cpp` — `ConfigStoreGetBinary`: depot-key injection + ConfigStore capture. (above)
  - `Hooks_Manifest.cpp` — `BuildDepotDependency`: patch `DepotEntry.{ManifestGid, ManifestSize}` post-call (short-circuit only if result true).
  - `Hooks_Package.cpp` — `CheckAppOwnership`: fake-license injection + `MarkOwned` dedup; `GetPackageInfo` capture; `CUtlMemoryGrow`/`MarkLicenseAsChanged`/`ProcessPendingLicenseUpdates` resolved; `NotifyLicenseChanged` → `MarkLicenseAsChangedAndProcessUpdates`. **Full fake-license pipeline above (F-1).**
  - `Hooks_Misc.cpp` — `GetAppIDForCurrentPipe` capture (→ `g_steamEngine`), `SpawnProcess` int3-trap (**480 rewrite**), `OptedInMask` 480→real, overlay `CGameID` rewrite, `GetAppDataFromAppInfo` capture (CAppInfoCache).
  - `Hooks_IPC.cpp` + `Hooks_IPC_ISteamUser.cpp` + `Hooks_IPC_ISteamUtils.cpp` — `IPCProcessMessage` hook: handshake (sets `pipe->m_clientPID`), interface-call dispatch (pre → original → post). Registered: `GetSteamID` (post, DenuvoAuth-window gated), `GetAppOwnershipTicketExtendedData` (post; DenuvoAuth window → `CredentialStoreOnly`, else `ForgeOnly`; `kOnlineFixAppId`→real rewrite; writes offsets+signature into resp), `RequestEncryptedAppTicket` (post; Records hAsyncCall→appId via `PendingAPICalls`), `GetEncryptedAppTicket` (post; serve cached eticket, buffer grow), `GetAppID` (post; 480→real), `GetAPICallResult` (post; callback table → `EncryptedAppTicketResponse` OK).
  - **`Hooks_NetPacket.cpp`** — the wire-replay layer (see F-4). 16 handler namespaces; send `BBuildAndAsyncSendFrame` + recv `RecvPkt` pooled replacement; eMsg send: 151 service-job (fnv of `target_job_name`), 8903 PICS, 742/5410 games-played, 7501 RP-upload, 818 get-userstats, 5466 store-userstats2; eMsg recv: 147 service-job, 819 stats-response, 9406 family-sharing, 766 persona-state; `g_SuppressSend` drops frames (cloud answered locally); packet pool 8×64 KB.
  - `Hooks_KeyValues.cpp` — `ReadAsBinary` KV-tree manipulation entry point (`KeyValuesSystemSteam` from vstdlib).
  - `Hooks_CallBack.cpp` — `SendCallbackToPipe` callback modifier dispatch.
  - `Hooks_SteamUI.cpp` — `FillInAppOverview`, `BuildCompleteAppOverviewChange`, `CSteamUIAppControllerRunFrame` (library-UI manipulation).
- **steam-monitor branches (rev 5):** three channels, all SHA-256-of-DLL-named:
  - `pattern/` — `steamclient/<sha>.toml` + `steamui/<sha>.toml`; schema `[0x<funcHash>] name, rva, sig` (e.g. 24 functions incl. `BuildDepotDependency` 0xC37F2D8E, `LoadDepotDecryptionKey` 0xB13C0C3F, `GetDecryptionKey` 0xD51873C5, `CheckAppOwnership` 0x4B1B1D77, `IPCProcessMessage` 0xC3E20E29, `GetAppIDForCurrentPipe` 0xA185DB47 with `??` wildcards).
  - `ipc/` — **richer**: per-interface `[IClientX] interface_id, vtable_rva`; per-method `method_index, funcHash, wrapper_rva, argc` (+ `fencepost`). Consumed by `IPCLoader::{Load, Find(interfaceID, funcHash), Find(ifaceName, methodName)}` BEFORE IPC hooks install.
  - `protobuf/` — full Steam client `.proto` archive: `encrypted_app_ticket.proto` (`ticket_version_no=1, crc_encryptedticket=2, cb_encrypteduserdata=3, cb_encrypted_appownershipticket=4, encrypted_ticket=5`), `offline_ticket.proto`, `timedtrial.proto` (`TimedTrial { GetTimeRemaining, RecordPlaytime, ResetPlaytime }`), ~90 steammessages/webuimessages files.
- **LuaConfig hot-reload mechanics (rev 5):** per-file tracking (`g_fileDepots`, `g_fileManifestOverrides`, `g_fileParseSequence`, `g_depotRefCount`, `g_fileMtime`, `g_purchaseTime` = max contributing file mtime); `RebuildManifestOverride` picks highest parse-sequence file per depot; pending additions/removals consumed by license-change notify; `kDefaultStatSteamId = 76561198028121353ULL` (hardcoded fallback when no setStat/stats API). Case-insensitive funcs = lowercase `_G` set + `g_func_registry` map + `__index` metamethod on `_G`.
- **Pipeline activation (rev 6, `PipeManager.cpp` + `ProcessInspector.cpp`):** the *exact* trigger F-7/F-1 bind to. `OnHandshake(pipe)`:
  - PipeKey from handshake (PID); snapshot resolved once per `(pid, creationTime)` and **cached** so sibling pipes reuse it (`TryReuseCachedProcess`); `ResolveProcess` = `InspectProcess(pid)` reads image path/name + env vars `SteamAppId` (appid), `SteamGameId`/`SteamOverlayGameId` (u64→`&0xFFFFFF`), flags `steamClientProcess` (name in `kSteamProcessNames`) and `likelyGameProcess = !steamClientProcess && HasSteamAppEnvironment()`.
  - `appId = snapshot.ResolveAppId()`; `trackedApp = appId != Invalid && LuaConfig::HasDepot(appId, false)`; `owned = trackedApp && IsOwned(appId)`.
  - Feature side effects run **off the registry lock**, in order: `DenuvoAuth::Apply(ctx)` then `Injection::Apply(ctx)`. **This OnHandshake → trackedApp/owned → Apply chain is F-7's gate moment and F-1's trigger.**
- **ProtectionScan exact algorithm (rev 6, `ProtectionScan.cpp`):**
  - Module filter: `.exe`/`.dll` only; `size ≥ kMinPackedModuleBytes = 80 MB` (fast-path skip; PR #121 additionally always scans the main exe); skip non-exe system paths; skip 10 Steam-runtime names (steamclient(.64), steam_api(.64), tier0_s(.64), vstdlib_s(.64), gameoverlayrenderer(.64)); `inGameTree` = path under first exe's directory. Sort: exe first → in-tree → size desc.
  - Detection precedence: `TryOepPattern` (scan only the OEP's containing section for the 7-byte `DODENUVO` camel23 `48 B9 44 4F 44 45 4E 55 56 4F`) then `FindLegacyDenuvoSection` (`.arch .srdata .xpdata .xdata .xtls`) + `DENUVO` string. Streaming 8 MB chunked BMH reads from disk (no full-module RAM). DetectionMatch carries method/sectionName/entryPointRva/matchRawOffset/matchRva.
- **Injection sidecar (rev 6, `Pipe/Features/Injection/Injection.cpp` + `OSTPlatform/Windows/RemoteProcess.cpp`):** completely separate from pipe features — config-gated extra injected DLL for games needing a native helper:
  - `InjectionSettings{enabled, libraryX64, libraryX86}`; `RemoteProcess::GetArchitecture` (IsWow64Process) picks the lib; skip unless `ctx.gameProcess` and not already-injected (dedup set keyed by ProcessKey).
  - Classic primitives: `OpenProcess(PROCESS_CREATE_THREAD|QUERY_INFORMATION|VM_OPERATION|VM_WRITE|VM_READ)` → `ResolveRemoteLoadLibraryW` (snapshot modules, `ResolveRemoteExport` walks forwards incl. api-ms-win→kernelbase, depth≤8) → `VirtualAllocEx` (RW path) → `WriteProcessMemory` → `CreateRemoteThread(LoadLibraryW)` → `WaitForSingleObject` (10 s) → `GetExitCodeThread`≠0. KInjectAccess constant captured for F-1's port.
- **RemoteToml fetch mechanics (rev 6, `SteamMetadata/RemoteToml.cpp`):** `Fetch(Request{channel, component, dllPath})` → SHA-256 of DLL → cache file `<steamroot>\opensteamtool\{channel}\{component}\{sha256}.toml` (created dirs) → mirror chain: configured `remote.url_template` (must contain `{channel}/{component}/{sha256}` placeholders, validated) else default `{GitHub-raw, jsDelivr}`; **404 = all mirrors serve same data → stop (don't try next)**; other HTTP errors → next mirror; 200 → write cache + return body. Feeds F-2's pattern fetch verbatim.
- **NetPacket send/recv decode (rev 6, full):** two delivery mechanisms plus a third:
  - `ReplaceSendPacket(repl body)` / `ReplaceRecvPacket(hdr, body)` → pool rebuild (8×64 KB); `g_ResizedInPlace` (819 body-shrink writes into the **original** pBody buffer then fakes `m_cubData`); **carrier-borrow** (see F-4): `TryInject`/`Drain` swap `pCarrier->m_pubData`→prebuilt packet for exactly one `oRecvPkt` call then restore (used for manufactured 766 persona + Cloud 147 responses; cloud queue capped at 64).
  - `SendJob` dispatch confirms: 151 → `SendServiceJob` by FNV1a of `target_job_name` (`GetUserStats`, `GetManifestRequestCode`); `Cloud.*` prefix → `CloudRedirectHost` handle-or-suppress (`g_SuppressSend`, two notif jobs passed through); 8903 patches `CMsgClientPICSProductInfoRequest` app rows in place (inject access_token per HasDepot+has-token, warn on "in depot, no token"); 742/5410 OnlineFix patches `game_extra_info` name for 480; 7501 RP-Upload → KV1 parse + re-inject staging; 818 rewrites `steam_id_for_user` + forces `schema_local_version=-1`; 5466 notifies stats-stored.
  - `RecvJob`: 147 response (jobid_target→appid correlator, eresult→OK), 819 clear_stats/achievement_blocks + `eresult=1` + CR overlay + crc recompute (+6-block injection with XOR crc), 9406/1183 FamilySharing body→empty, 766 in-place persona patch (or clears RP bit on no-KVs launch).
  - **`5527` (encrypted-app-ticket response)**: commented out in netpacket (`// migrated to IPC Layer`); the live eticket response path is `Hooks_IPC_ISteamUser::GetEncryptedAppTicketResponse` (below).
- **IPC handler detail decode (rev 6, `Hooks_IPC_ISteamUser.cpp`):** all four `IClientUser` + two `IClientUtils` post-handlers registered via `ADD_IPC_POST_HANDLER`:
  - `GetSteamID` — noop unless `PipeManager::DenuvoAuth::IsAuthorizedPipe(pipe)` (authorization window); else `AppTicket::GetSpoofSteamID(appId)` into returnValue.
  - `GetAppOwnershipTicketExtendedData` — guard `cbMaxTicket < 0`; `unAppID == kOnlineFixAppId ? ResolveAppId() : unAppID`; ticketSource = `CredentialStoreOnly` (in auth window) **else `ForgeOnly`**; writes `returnValue=totalSize`, `piAppId/piSteamId/piSignature/pcbSignature` offsets + ticket bytes.
  - `RequestEncryptedAppTicket` — reads the hAsyncCall Steam already wrote, records `PendingAPICalls::RecordEncryptedTicket(hAsyncCall, appId)`; the w/ `GetAPICallResult` (ISteamUtils post) maps that back to the 5527-ish response.
  - `GetEncryptedAppTicket` — serves cached credstore eticket: `Hooks_Misc::EnsureBufferCapacity(pWrite, cap+ticket)` (oEnsureBufferCapacity + `m_Put = newCapacity`), then `returnValue=true, pcbTicket, pTicket`.
- **steam-monitor branch model (rev 6, confirmed):** three **git branches** (`ipc`, `pattern`, `protobuf`); default branch serves the protobuf archive. `pattern/` = 44 `steamclient/<sha>.toml` + 45 `steamui/<sha>.toml`; `ipc/` = per-interface `[IClientX]{interface_id, vtable_rva}` + per-method `method_index/funcHash/wrapper_rva/argc/fencepost` (sample `0e68d65…toml`: IClientUser.id=1 vtable 0x12EBCE8; GetSteamID idx10 hash 0xD6FC3200 wrapper 0x782570; GetAppOwnershipTicketExtendedData idx105 hash 0xC7E71245 wrapper 0x74AAF0; RequestEncryptedAppTicket idx120 hash 0x25D6BB1D wrapper 0x83F9E0; GetEncryptedAppTicket idx121 hash 0xE0468CB4 wrapper 0x75D9B0; IClientUtils.id=4 vtable 0x12F1720, GetAppID idx19, GetAPICallResult idx24 hash 0x2D3D3947 wrapper 0x745690). F-2's `IPCLoader` consumes exactly these.
- **Game-name resolution (rev 6, `Hooks_Misc::GetGameNameByAppID`):** **no HTTP** — `oGetAppDataFromAppInfo(g_pCAppInfoCache, appId, "common/name", buf, 256)`; keyType is implied by `"common/"` prefix (keyType=2 → tries `name_localized/<lang>` then falls back to `name`, returns `strlen+1` / −1 on miss); per-app cache. Used by OnlineFix (480→real name) and RP (`game_name` in 766 persona). F-4/F-8 port this instead of any web call.

---

## 11. GabLuchi backend anatomy — reuse, not rebuild (owner decision)

Confirmed live services (recorded in `infos.md §8`):

| Key | Value | Role |
|---|---|---|
| `ManifestBackendBase` | `http://167.235.229.108` | own manifest check backend — `GET /check_apis?appid=` UA `secretgoonpoon` |
| `ApiBaseUrl` | `https://lua.tools` | DLC-lua + Denuvo fix listings — `/api/dlc/info?appid&base=`, `/api/dlc/generate`, `/api/denuvo/listings` (public), `/api/denuvo/fixes?appid=` |
| `AuthBackendBase` | `https://gabluchi-auth.freestuffsyeah65.workers.dev` | Cloudflare Worker — Discord OAuth PKCE exchange |
| `KeyCheckerBase` | `https://gabluchi-proxy.freestuffsyeah65.workers.dev` | Cloudflare Worker — `/activate`, `/manifest/{appid}?token=`, `/account` |
| `HubcapBaseUrl` | `https://hubcapmanifest.com` | manifest cloud (Bearer key, daily limits) |
| `DiscordClientId` | `1535479561158918216` | Discord OAuth |

- **License flow (shipped — F-7 starts here):** key `XXXXX-XXXXX-XXXXX-XXXXX` (19 chars, dashes 4/9/14); `machineId = SHA256(volumeSerial|MachineName|UserName)`; POST `{KeyCheckerBase}/activate {key, machineId, discordUserId}` → `{token}`; token DPAPI-encrypted; `GetDownloadUrl(appid) = {KeyCheckerBase}/manifest/{appid}?token=` — **manifest downloads ALREADY license+token+HWID gated**.
- **Auth:** Discord OAuth PKCE S256 (`identify guilds`), `http://localhost:53789/callback` HttpListener loopback, `auth.dat` under `%APPDATA%\GabLuchi`, guest mode. Stored `{Token, ExpiresAt, UserId, DisplayName, AvatarUrl}` + startup `ValidateAsync`; all downloads stage via `%TEMP%\GabLuchi\downloads`.
- **Hubcap API:** `GET /api/v1/user/stats?api_key=`; `GET /api/v1/status/{appid}` (Bearer) → `{status, manifest_file_exists}`; `GET /api/v1/manifest/{appid}?api_key=` → `{appid}.zip`; errors 401 invalid/expired, 429 daily limit, 404 no manifest.
- **Local RPC `127.0.0.1:6767` (HttpServerService):** `/health/scan-status`, `/health/scan-all` (POST), `/health/av-status`, `/health/{appid}`, `/health/{appid}/repair` (POST), `/open-url`, `/api-list` (+ netsh urlacl). → **F-2/F-11 extend this, don't copy Mugi.**
- **Millennium plugin surface (F-3/F-5 target):** current plugin API is **LuaJIT-backed** (`docs.steambrew.app` — Python deprecated): `ready()`, `version()`, `steam_path()`, `get_install_path()`, `get_plugin_logs()`, `add_browser_css/js`, `remove_browser_module`, `get_user_settings`/`set_user_settings_key`, `call_frontend_method`, `change_plugin_status`, `is_plugin_enabled`, `cmp_version`. Promise-style factory model. `gabluchi-plugin` scripts against these (detail in `infos.md §10`).
- **ConnectRelay** (Go, in-repo): ws lobby relay — 6-char codes, 5-min TTL, 10 req/min, 10 conns. Keep for F-4 lobby coordination.
- **Live probes (this session):** `/api/denuvo/listings` public (tags `DenuvOwO` + `SteamTools Achievements Fix` + `voices38 (crack)`); `/api/dlc/info|generate` → `{"error":"Unauthorized"}` anonymous; `/api/denuvo/fixes?appid=1091500` → `No fixes found`.
- **Fix flow status (rev 10, 2026-09-20):** GabLuchi "Fix applied/Apply" for `DenuvOwO`/`voices38` is broken by design — the Fix button downloads the **manifest** (dead `fallbackName`; both slots call `DownloadManifestAsync(appid,"Ryuu")` → `167.235.229.108/<appid>`), then unzips it into the game dir and toasts success. Fix artifacts **have a route — `lua.tools/api/denuvo/download?fix={uuid}&slot=fix`, session-gated** (Supabase session → pre-signed R2 URL, 120 s, `filename={appid}.zip`; confirmed by HAR 2026-09-20). Fix spec now in `PATCH.md §5`: shared ref-token session + `GetDenuvoDownloadAsync` + fix-slot rewiring (repo-cache → API → stream → 7za → ApplyToGame), dead `fallbackName` removed, validation per rev-11 rules. Worker `/fix` + park-zips design is the deferred fallback only (`PATCH.md §5.4`). Keep: 7za extraction + `.bak` + tag-aware post-apply + `CrackFixService` buzzheavier misroute + missing-7za toast (unchanged hygiene scope). Informed-choice gate for DenuvOwO stays mandatory (§12). **Fix-zip validation must be per-fix expected-file lists, not a single hardcoded check** — HV packages are multi-binary (`.org` proxy, `steam_api64.dll`, `hypervisor-launcher.exe`, `watchdog.exe`, `VBS.cmd`, V2 `steamclient_loader_x64.exe`), files can live in a subfolder not the game root (Crimson Desert `bin64` lesson), and `KIRIGIRI.bin` is **runtime-created on first launch — must NOT be required in the validation list** (rev 11).
- **voices38 scene status (rev 9, 2026-09):** the `voices38 (crack)` tag already coexists with `DenuvOwO` in our public listings — good, that's the two-roads model (HVB hypervisor vs proper crack) in data. Update: Denuvo sued voices38 (2026-09-14, N.D. Cal, §1201, 26 games, injunction + unmasking via Reddit/Discord/Steam accounts). Keep both tags forever, never re-tag crosses, and treat `voices38` entries as supply-watch (**rev 11.1: the lawsuit did NOT slow releases as of 09-20** — 5-crack burst 09-06 after a 3-week break, explicit "everything will continue as normal"; real risk is future unmasking via Valve/Reddit/Discord subpoenas, not release volume). `HyperDevil` (DevilJinOfficial) bundles both file sets with SHA-256 — reference as the UX shape for a future combined browse page.
- **DenuvOwO scene intel (rev 11, 2026-09-20):** group = KiriGiri, Andreh, 0xZe0n, sagerao (lineage: MKDEV TEAM + SpecialFor drivers — the RE Requiem V2 NFO is `READNFO-MKDEV TEAM.txt`; cs.rin.ru approves HV releases from DenuvOwO only). Our listings tag **3** DenuvOwO games (`3321460` Crimson Desert ×3 fixes, `1285190` Borderlands 4, `2246340` Monster Hunter Wilds); **Crimson Desert is the only dual-tag** (both `DenuvOwO` + `voices38 (crack)`) → it's the canonical two-roads test case. Technical model: `KIRIGIRI.dll` picks `SimpleSvm.sys` (AMD) or `hyperkd.sys`+`hyperhv.dll` (Intel, HyperDbg 762-fn fork) → service `denuvo_kirigiri` → spoofs CPUID/MSR/RDTSC/KUSER/KdDebuggerNotPresent; token lifecycle = 2 pre-generated tokens per CPU arch, written to `KIRIGIRI.bin` on first launch, `CreateFileW` IAT redirect on license ID `92346205896`; the ~"Feb 2027" expiration IS the embedded ASN.1 fake-license cert window `260226065959Z→270226071959Z` (audit §6.4/B.5). Launch base = detanup01's GBE fork + HyperEvade; V2 = ColdClientLoader/`steamclient_loader_x64.exe`, V3 = VBS.cmd (v1.2, 2026-03-23) + Steam launch. DSE has **multiple paths** — built-in `0xDEADC0DE` UEFI-variable bypass (test-signing/CI.dll patch), EfiGuard optional bootkit, VBS.cmd one-shot F7. Threat model (§12): Win11 kernel driver-trust revocation (MS blog 2026-03-26 + Apr-2026 update; eval→enforcement ~100 h uptime + 2–3 reboots) will eventually hard-block; Irdeto building non-ring-1 countermeasure; 2K titles immune via bi-weekly online DRM check. Scene log (rev 11.1): Onimusha: Way of the Sword 09-04 HV bypass day-one (Capcom; voices38 full crack still pending), Echoes of Aincrad 07-10, 007 First Light 1.0.5 (build 23685521) 06-16 — **dual-road games exist** (007 has both DenuvOwO HV and voices38 versions in the scene; feed tags voices38 only).
- **Online fixes:** `OnlineFixEntry{AppId, GameName, FileName, SizeBytes, DownloadUrl, Source="perondepot", Password="online-fix.me"}`.
- **Distro/mirror:** GH mirrors `ghproxy.net`/`ghfast.top`/`gh.ddlc.top`; plugin in separate `gabluchi-plugin` repo; `Steamless` + `Selectively11/CloudRedirect` pinned.

---

## 12. Denuvo policy — metadata + informed-choice only (FINAL)

Owner decisions 2026-09-19: pool-mint **DEAD** (no owned games → etickets prove ownership that doesn't exist); DenuvOwO is a manual risk-heavy detour, not a feature.

- **What DenuvOwO is:** hypervisor bypass (MKDev/Kirigiri). Unsigned ring -1 driver (AMD `SimpleSvm.sys` / Intel `hyperkd.sys`), OS+Denuvo in a guest VM, spoofs CPUID (clears hypervisor bit + zeroes vendor leaves), MSR, RDTSC/RDPMC, syscalls (LSTAR/EFER), EPT memory hooks (Denuvo reads clean / executes patched), RFLAGS.TF forwarding.
- **Why it is NOT a product feature:** requires disabling VBS/HVCI/Credential Guard/System Guard/Windows Hello + one-shot F7 DSE (or the package's built-in `0xDEADC0DE` UEFI-variable DSE bypass, or optional EfiGuard bootkit — DSE-off is non-negotiable however you get there) + PowerShell execution policy (script forgets to restore); per-release files must match exact build; **license expiry = embedded ASN.1 fake-license certs `260226065959Z → 270226071959Z` (expires 2027-02-26)** — the "~Feb 2027" clock; fake bundle `KIRIGIRI.bin` (runtime-created, CPU-vendor-specific) served via `CreateFileW` IAT redirect on license ID `92346205896`; **6 published flaws = PoC (0-x-0-0/denuOwO-hypervisor-vulnerabilities), NOT CVE-assigned, no admin to trigger** (CR3 impersonation, write-what-where `CPUID[0x336933]`, exception-handler overwrite ring3→ring0, VMLOAD race, unbounded CPUID, KASLR leak `CPUID[0x41414141]`); AMD vs Intel use different drivers; older-Intel instability.
- **App surface:** classify fixes into 4 types in the Denuvo listing UI — **HVB bypass** (manual "gateway + risk card", opens `DENUVOWO_GUIDE.md`, never auto-apply), **proper crack** (voices38-style, prefer/link), **offline activation** (hardware-bound token), **online fix**. No in-app minting, no automatic HVB deployment.
- Companion guide shipped: `DENUVOWO_GUIDE.md`.