# DenuvOwO Hypervisor Bypass — Complete Step-by-Step Guide

**What this is in one line:** a tool that slides a hypervisor under Windows, lets Denuvo keep running, and lies to it about everything it checks — so the game launches like you own it.

This is a manual, high-risk workflow. GabLuchi will never auto-apply this — the app only opens this guide on an informed-choice gate.

**Scene status (2026-09):** the hypervisor route is not the only road — see Part 12 for the alternatives. The leading "proper crack" source, **voices38**, was sued by Denuvo on 2026-09-14 (N.D. California, §1201 anti-circumvention, 26 games; seeks injunction + unmasking) and has replied "everything is fine / will continue as before." Supply risk is real: if his releases slow or dry up, demand shifts onto this hypervisor path, and his cracks may vanish from the usual channels. Treat both roads as moving targets — VPN + trustworthy mirrors only.

---

## Part 1 — Before you even download anything

- **Check your CPU.** Task Manager → Performance → CPU → look at the name. AMD Ryzen → your driver is `SimpleSvm.sys`. Intel Core → your driver is `hyperkd.sys` (+ `hyperhv.dll`). The two are *different programs*; guides that say "works great" may be for the other brand. It's installed as a kernel service named `denuvo_kirigiri`.
- **Know which generation you have.** V3 (current releases) = `hypervisor-launcher.exe` + `VBS.cmd`, launched from Steam. V2 (older, MKDEV-style — e.g. Resident Evil Requiem) = `steamclient_loader_x64.exe` (ColdClientLoader + Goldberg emulator, no Steam). **Each release's NFO tells you which runner — read it before anything.**
- **Check the requirements.** Windows 10 21H2 or Windows 11. Intel 4th gen or newer, AMD Ryzen 1st gen or newer. 64-bit, obviously.
- **Turn on virtualization in the BIOS** (if the script errors with "no SVM/VT-x"): reboot → mash F2/Del → find "SVM Mode" (AMD) or "Intel Virtualization Technology / VT-x" (Intel) → Enable → Save & Exit.
- **Expect a reboot marathon.** This method reboot-runs through a one-time boot menu. Set aside 15–20 min.
- **Back up anything precious.** A few files in your Documents you can't lose? Copy them to a USB or cloud. BitLocker suspension may trigger a recovery-key prompt (Part 6).

## Part 2 — Get the game + the bypass files

- **Repack path:** the game usually comes bundled with a `DenuvOwO` folder inside. Sources that test before releasing: cs.rin.ru, FitGirl, DODI. If the archive has a setup.exe → install it, then a `DenuvOwO` folder shows up next to the installed game.
- **Genuine install path:** you own the game and want to bypass instead? Download the release's DenuvOwO patch (usually a `DenuvOwO.7z`), extract it, and you'll place its contents into the game folder per Part 4.
- **Verify you actually unzipped everything** — the small files hide. Windows Defender sometimes quarantines the `.sys` driver; if the script later fails, check Protection History and "Allow" the item, then re-extract.

## Part 3 — Install the game (repacks)

- Run `setup.exe` as administrator (right-click → Run as administrator).
- Install to a path with no spaces issues, e.g. `C:\Games\CrimsonDesert`.
- Do **not** launch it yet. Do not run any crack/emu yet. We do the DenuvOwO route *instead* of SteamTools for this game.

## Part 4 — Copy the bypass files (the part people get wrong)

- Open the extracted `DenuvOwO` folder. You'll see things like `hyperhv.sys`, `hyperhv.dll`, `hyperevade.dll`, `hypervisor-launcher.exe`, `VBS.cmd`, and a release-files folder. (V2 releases instead ship `steamclient_loader_x64.exe` + a `coldclient` folder.)
- **Check the `VBS.cmd` version before trusting it** — current releases use v1.2 (2026-03-23). If you're running an older zip, grab a fresh one; the script changes with each Windows driver-trust update.
- **Watch for `.org` proxy files** — releases often rename the original DLL (e.g. `amd_ags_x64.org`) and drop a replacement `.dll` that auto-loads the bypass. Both are normal; don't delete one half of the pair.
- **Don't expect `KIRIGIRI.bin` in the zip** — it's created next to the game exe on first launch (the fake Denuvo license, written once, CPU-vendor-specific). If you see it before first launch, the fix is already applied.
- **Delete the launcher/`.exe` files that came with the game's original DRM folder** if the instructions say so (each release NFO says exactly which files to remove).
- **Copy the DenuvOwO contents into the folder that contains the game's main .exe** — this is *not always the root*. Example that burns everyone: Crimson Desert → copy into `bin64`, not the install root.
- Click "Replace / Yes to All" when Windows asks.
- The general checklist: the folder with the actual game executable needs `hypervisor-launcher.exe` + the release files next to it, and the `.sys`/`.dll` hypervisor files can just live in the same folder too.

## Part 5 — Run the setup script

- **How DSE actually gets disabled (pick whichever fits the release):** newer V3 packages do it **at game launch** via a built-in UEFI-variable patch (looks up `g_CiEnabled`/`g_CiOptions` in the CI DLL and flips them — magic `0xDEADC0DE`), and some ship an optional **EfiGuard** bootkit that disables DSE + PatchGuard at boot. The `VBS.cmd` route is the convenience option that schedules the one-shot boot so you don't have to think about any of that. All roads lead to "Driver Signature Enforcement off" — none are optional to skip.
- Right-click `VBS.cmd` → **Run as administrator**.
- A black window appears with numbered options.
- **Press 1** (prepare: disables Virtualization-Based Security, Memory Integrity/HVCI, Credential Guard, System Guard, and schedules the EFI tamper on UEFI-locked machines). It logs everything under `HKLM\SOFTWARE\ManageVBS` so it can undo it all later.
- It may insert a one-time boot entry to strip UEFI-locked settings → **reboots are automatic here. Let it restart.**

## Part 6 — The first reboot: driver signature disabled (do NOT panic)

- If it boots to the blue "Choose an option" startup screen: **that's the script working, not a crash.**
- **Press F7** — labeled "Disable driver signature enforcement" (label varies by Windows version; it's the one that is *not* "safe mode").
- This is a **one-shot**: it only applies to this single boot. Next reboot, driver signatures are enforced again automatically. You won't stay exposed.
- **BitLocker users:** if you get the BitLocker recovery screen instead, the script suspended protection — either let it proceed (key stored in your Microsoft account) or accept that you can't use this method on that machine.

## Part 7 — Double-check two switches before launching

- The script is usually thorough, but verify:
  - **Reboot → Windows Security → Device Security → Core Isolation → Memory Integrity → ON state? Turn it OFF** if the script didn't.
  - **(Advanced)** If the game still fights you on some CPUs, `bcdedit /set hypervisorlaunchtype off` in an admin Terminal (you'll re-enable on cleanup, Part 10).

## Part 8 — Launch the game

- **Per-release NFO is authoritative on the launcher.** V3: the game's launcher `hypervisor-launcher.exe` expects — often the game exe via Steam or a bundled `Launch.bat` — launches non-admin (KIRIGIRI steals an Explorer token to spawn it). V2: you run **`steamclient_loader_x64.exe`** yourself; Steam isn't involved at all (Goldberg answers the Steamworks calls locally). If you launch the wrong one, the game exits silently. **(Rev 11.1) The listing tag is NOT exhaustive:** e.g. 007 First Light has a DenuvOwO HV bypass (1.0.5, build 23685521, 06-16) *and* voices38 cracks in the scene, yet the feed tags it `voices38` only — resolve method from the release's NFO, never from the feed tag.
- Right-click → **Run as administrator** (V3) — V2's loader, the NFO will say whether it needs elevated too.
- First launch may be slow — the hypervisor boots Windows-in-a-VM around the game. Give it a minute before calling it hung.
- You'll see the process tree: game exe runs as a guest VM while the hypervisor answers Denuvo's questions with lies (CPUID, MSR, RDTSC timing, syscall results, memory integrity reads).

## Part 9 — While you play / between sessions

- **Don't reboot mid-game with the game running.**
- Works per-boot: each time you want to play DenuvOwO-style you reboot → F7 → run the launcher.
- **Watch the clock.** The game's license is faked: the bypass writes a counterfeit Denuvo token to **`KIRIGIRI.bin`** (first launch) and redirects Denuvo's license-file reads to it via a `CreateFileW` hook. That fake certificate is only valid **2026-02-26 → 2027-02-26** — when the date passes, the game stops launching even on valid systems. You need a fresh release from the group. Same if the game updates its build.

## Part 10 — Cleanup & restore (do this when a session ends)

- Close the game (normal exit, let the hypervisor unload — it auto-stops `ControlService` and removes the driver service).
- **Run `VBS.cmd` as admin again → press 3** (restore). It re-enables exactly what Part 5 disabled, tracked in that registry key.
- **Reboot** so the changes apply.
- **Fix the one thing the script forgets** — it loosens the PowerShell execution policy and never puts it back. Run in an admin PowerShell:
  ```
  Set-ExecutionPolicy Restricted
  ```
  Then press `Y`. Ten seconds, don't skip it.
- **Re-enable Core Isolation → Memory Integrity** if you disabled it manually (`Windows Security → Device Security`).
- If you ran Part 7's advanced step: `bcdedit /set hypervisorlaunchtype auto` in admin Terminal and reboot.
- Confirm done: Sign in normally, no boot menu, Driver Signature Enforcement is back on (you can't launch the game anymore until the next F7 session — that's expected).

## Part 11 — Troubleshooting

- **Game won't start / silent exit:** driver didn't match your CPU brand (AMD vs Intel — redownload accordingly), you installed files in the wrong folder (check Part 4's `bin64` lesson), you used the wrong runner (V2 `steamclient_loader_x64.exe` vs V3 Steam launcher — reread the NFO), or the expired-clock (Part 9).
- **Windows 11 warning (rev 11.1):** the Apr-2026 update (MS blog 2026-03-26) stops trusting the cross-signed-driver program — two-phase: evaluation mode logs blocked events first, then auto-enforcement after ~100 h uptime + 2–3 clean reboots. That auto-blocks the old cross-certificate *drivers* the DSE-Patcher route relied on — the hypervisor method is effectively dead on fully-updated Win11 installs. If the driver won't load or the game won't boot past the black window, this is likely why. Assessment-mode / group-policy juggling won't save you; a blocked machine needs a release bundled with a current driver. **voices38-style proper cracks are unaffected** — they don't need security-feature disables (heise 09-17).
- **Blue screen during launch:** unstable on older Intel (known), or two hypervisors fighting — re-run VBS.cmd option 3, confirm your hypervisorlaunchtype, retry on a cleaner boot.
- **Defender ate the driver:** restore from quarantine, re-extract, and add an exclusion for the game folder before re-running.
- **"SVM/VT-x not available":** enable in BIOS (Part 1).
- **Determining the problem:** the group's NFO per-release lists known-good CPU/config combos and which folders to drop files into — read it before pulling your hair out.

## Part 12 — The safer roads (worth mentioning, seriously)

- **Proper crack** (voices38-style): a normal install-and-play, Denuvo fully stripped. No hypervisor, no F7, no security toggle roulette. Best choice when available — your `lua.tools/denuvo/listings` tags these separately from HVB.
- **Offline activation tokens** (AntiDenuvo Sanctuary-style): you join a community Discord server, request a hardware-bound token, apply it, and play with *zero* changes to system security. Fragile (breaks if you update Windows, change GPU, or reinstall) but far safer than playing with one boot's worth of unsigned drivers.

**Closing statement:** The hypervisor route works, but it's a chore with a clock on it — expect ~30–60 seconds of setup ceremony before every play session, an expiry built into every release, and a security posture that's only as good as the author's driver hygiene. When the plan lands, GabLuchi's Denuvo surface links to this guide on an informed-choice gate and never auto-applies any of it.