# ============================================================
# GabLuchi Production Release Script (v1.3.9+)
#
# One-shot: version check -> clean -> build -> pack -> BOM strip
#   -> verify -> commit/push/tag -> gh release create -> VISIBILITY GATE
#
# Usage:
#   .\release.ps1 -Version 1.3.9 -Notes "Release notes here"
#
# The version must ALREADY match in BOTH GabLuchi.csproj and
# Properties/AssemblyInfo.cs. The script verifies this and fails fast.
# It does NOT edit version files for you.
#
# Optional: -CommitMessage  default: "v$Version - $Notes-first-line"
# Optional: -NoPush          build/pack/verify only, no git/gh steps
# ============================================================
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter(Mandatory = $true)]
    [string]$Notes,

    [Parameter(Mandatory = $false)]
    [string]$CommitMessage,

    [switch]$NoPush
)

$ErrorActionPreference = "Stop"
$repo = "Gab-lutang/gabluchi"
$repoUrl = "https://github.com/Gab-lutang/gabluchi"
$expectedAssets = 6

function Step-Host([string]$msg) { Write-Host ""
    Write-Host "=== $msg ===" -ForegroundColor Cyan }

function Fail-Script([string]$msg) {
    Write-Host "FAIL: $msg" -ForegroundColor Red
    exit 1
}

function Test-Command([string]$name) {
    if (-not (Get-Command $name -ErrorAction SilentlyContinue)) {
        Fail-Script "'$name' not found on PATH. Aborting."
    }
}

# --- Preflight --------------------------------------------------
Test-Command "dotnet"
Test-Command "vpk"
Test-Command "gh"
Test-Command "git"

if ($Version -notmatch "^[0-9]+\.[0-9]+\.[0-9]+$") {
    Fail-Script "Version must be X.Y.Z (got '$Version')"
}

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root
$expected0 = "$Version.0"

# --- Version sync check (csproj vs AssemblyInfo) ----------------
Step-Host "Version sync check ($root)"
$csproj = Get-Content "GabLuchi.csproj" -Raw
if ($csproj -notmatch "<Version>$([regex]::Escape($Version))</Version>") {
    Fail-Script "GabLuchi.csproj <Version> is not '$Version'"
}
if ($csproj -notmatch "<AssemblyVersion>$([regex]::Escape($expected0))</AssemblyVersion>") {
    Fail-Script "GabLuchi.csproj <AssemblyVersion> is not '$expected0'"
}
if ($csproj -notmatch "<FileVersion>$([regex]::Escape($expected0))</FileVersion>") {
    Fail-Script "GabLuchi.csproj <FileVersion> is not '$expected0'"
}
$asm = Get-Content "Properties\AssemblyInfo.cs" -Raw
$patFileVer = 'AssemblyFileVersion\("' + [regex]::Escape($expected0) + '"\)'
$patInfoVer = 'AssemblyInformationalVersion\("' + [regex]::Escape($Version) + '"\)'
$patVer = 'AssemblyVersion\("' + [regex]::Escape($expected0) + '"\)'
if ($asm -notmatch $patFileVer) {
    Fail-Script "AssemblyInfo.cs AssemblyFileVersion is not '$expected0'"
}
if ($asm -notmatch $patInfoVer) {
    Fail-Script "AssemblyInfo.cs AssemblyInformationalVersion is not '$Version'"
}
if ($asm -notmatch $patVer) {
    Fail-Script "AssemblyInfo.cs AssemblyVersion is not '$expected0'"
}
$upd = Get-Content "GabLuchiUpdater\GabLuchiUpdater.csproj" -Raw
if ($upd -notmatch "<Version>$([regex]::Escape($Version))</Version>") {
    Fail-Script "GabLuchiUpdater.csproj <Version> is not '$Version'"
}
if ($upd -notmatch "<AssemblyVersion>$([regex]::Escape($expected0))</AssemblyVersion>" -or $upd -notmatch "<FileVersion>$([regex]::Escape($expected0))</FileVersion>") {
    Fail-Script "GabLuchiUpdater.csproj AssemblyVersion/FileVersion is not '$expected0'"
}
Write-Host "OK: csproj, AssemblyInfo.cs and GabLuchiUpdater.csproj all on $Version" -ForegroundColor Green

# --- Clean ------------------------------------------------------
Step-Host "Cleaning build artifacts"
Get-ChildItem -Recurse -Directory -Filter "bin" -EA SilentlyContinue | ForEach-Object { Remove-Item $_.FullName -Recurse -Force }
Get-ChildItem -Recurse -Directory -Filter "obj" -EA SilentlyContinue | ForEach-Object { Remove-Item $_.FullName -Recurse -Force }
Remove-Item "Release" -Recurse -Force -EA SilentlyContinue
Remove-Item "Releases" -Recurse -Force -EA SilentlyContinue

# --- Build ------------------------------------------------------
Step-Host "dotnet publish -c Release"
dotnet publish -c Release -o Release/publish
if ($LASTEXITCODE -ne 0) { Fail-Script "dotnet publish failed (exit $LASTEXITCODE)" }

if (-not (Test-Path "Release\publish\GabLuchi.exe")) {
    Fail-Script "GabLuchi.exe missing after publish"
}

# --- Updater -----------------------------------------------------
Step-Host "dotnet publish GabLuchiUpdater + bundle into app"
dotnet publish -c Release -o Release/updater GabLuchiUpdater/GabLuchiUpdater.csproj
if ($LASTEXITCODE -ne 0) { Fail-Script "GabLuchiUpdater publish failed (exit $LASTEXITCODE)" }
if (-not (Test-Path "Release\updater\GabLuchiUpdater.exe")) {
    Fail-Script "GabLuchiUpdater.exe missing after publish"
}
Copy-Item "Release\updater\*" "Release\publish\" -Recurse -Force
$v = Get-Item "Release\publish\GabLuchiUpdater.exe"
$vam = $v.VersionInfo.FileVersion
Write-Host "GabLuchiUpdater.exe bundled ($vam) at $($v.Length) bytes" -ForegroundColor Green

# --- Pack -------------------------------------------------------
Step-Host "vpk pack"
vpk pack --packId GabLuchi --packVersion $Version --packDir "Release\publish" --mainExe GabLuchi.exe
if ($LASTEXITCODE -ne 0) { Fail-Script "vpk pack failed (exit $LASTEXITCODE)" }

# --- Strip BOM from RELEASES ------------------------------------
Step-Host "Strip UTF-8 BOM from Releases\RELEASES"
$p = "Releases\RELEASES"
if (-not (Test-Path $p)) { Fail-Script "Releases\RELEASES not found after pack" }
$b = [IO.File]::ReadAllBytes($p)
if ($b.Length -gt 3 -and $b[0] -eq 239 -and $b[1] -eq 187 -and $b[2] -eq 191) {
    [IO.File]::WriteAllBytes($p, $b[3..($b.Length - 1)])
    Write-Host "BOM stripped"
} else {
    Write-Host "No BOM present"
}

# --- Verify releases.win.json ------------------------------------
Step-Host "Verify releases.win.json"
$feed = Get-Content "Releases\releases.win.json" -Raw | ConvertFrom-Json
$assets = $feed.Assets
if (-not $assets) { Fail-Script "releases.win.json has no Assets" }
$mismatch = $assets | Where-Object {
    $id = if ($_.Package) { $_.Package.Id } else { $_.PackageId }
    $ver = if ($_.Package) { $_.Package.Version } else { $_.Version }
    "$id-$ver" -ne "GabLuchi-$Version"
}
if ($mismatch) {
    Fail-Script "releases.win.json contains a non-GabLuchi-$Version entry: $($mismatch.Package.Id)-$($mismatch.Package.Version)"
}
Write-Host "releases.win.json lists $($assets.Count) asset(s) for $Version" -ForegroundColor Green

if ($NoPush) {
    Write-Host ""
    Write-Host "-NoPush set: stopping after build/pack/verify." -ForegroundColor Yellow
    exit 0
}

# --- Commit & push ----------------------------------------------
Step-Host "git commit + push ($repoUrl)"
$prevEAP = $ErrorActionPreference
$ErrorActionPreference = "Continue"  # git stderr (LF/CRLF warnings) isn't fatal
if (-not $CommitMessage) {
    $firstLine = ($Notes -split "`n")[0]
    $CommitMessage = "v$Version - $firstLine"
}
git add -A 2>$null
if ($LASTEXITCODE -ne 0) { Fail-Script "git add failed" }
git add -f "Releases\GabLuchi-$Version-full.nupkg" 2>$null
git commit -m $CommitMessage 2>$null
if ($LASTEXITCODE -ne 0) { Fail-Script "git commit failed" }
git push origin main 2>$null
if ($LASTEXITCODE -ne 0) { Fail-Script "git push origin main failed" }
git tag "v$Version" 2>$null
if ($LASTEXITCODE -ne 0) { Fail-Script "git tag v$Version failed" }
git push origin "v$Version" 2>$null
if ($LASTEXITCODE -ne 0) { Fail-Script "git push origin v$Version failed" }
$ErrorActionPreference = $prevEAP

# --- gh release create ------------------------------------------
Step-Host "gh release create v$Version"
$files = @(
    "Releases\GabLuchi-$Version-full.nupkg",
    "Releases\GabLuchi-win-Setup.exe",
    "Releases\GabLuchi-win-Portable.zip",
    "Releases\RELEASES",
    "Releases\releases.win.json",
    "Releases\assets.win.json"
)
foreach ($f in $files) { if (-not (Test-Path $f)) { Fail-Script "missing release file: $f" } }

gh release create "v$Version" --repo $repo --title "v$Version" --notes $Notes @files
if ($LASTEXITCODE -ne 0) { Fail-Script "gh release create failed (exit $LASTEXITCODE)" }

# --- VISIBILITY GATE --------------------------------------------
# GitHub REST index is eventually consistent: /releases list + /tags can
# serve assets:[] for a fresh release. Velopack snips releases with an
# empty embedded assets array -> clients report "no update available".
# Wait until the index embeds all $expectedAssets before announcing.
Step-Host "Visibility gate (waiting for REST index to embed assets)"
$scanStart = Get-Date
$timeout = New-TimeSpan -Minutes 30
$count = 0
do {
    Start-Sleep -Seconds 30
    try {
        $raw = gh api "/repos/$repo/releases/tags/v$Version" --jq '.assets | length'
        $count = [int]$raw
    } catch {
        $count = 0
    }
    Write-Host "embedded assets: $count / $expectedAssets"
    if ((Get-Date) - $scanStart -gt $timeout) {
        Fail-Script "TIMEOUT after 30 min - release still not visible. DO NOT ANNOUNCE."
    }
} while ($count -lt $expectedAssets)

Write-Host ""
Write-Host "RELEASE VISIBLE - safe to announce." -ForegroundColor Green
Write-Host "v$Version is live at https://github.com/$repo/releases/tag/v$Version" -ForegroundColor Green