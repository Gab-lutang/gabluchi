#!/usr/bin/env python3
"""
Generate unlisted.json for the Gab-lutang/gabluchi-search repo.

Pipeline (corrected 2026-10-01):
  1. Enumerate source repos -> universe of appids GabLuchi can serve
     (mirror + sushi; --eqhub adds EQhub's 62k branch appids).
  2. Load morrenus applist (380k entries incl. delisted) -> free names.
  3. storesearch probe per source game with its name: if the appid does
     NOT appear, the game is unlisted (GabLuchi's Add search can never
     surface it) -> survivor. Probe results append to a JSONL cache, so
     re-runs only probe what is new.
  4. Survivors: appdetails for canonical name + type == "game"
     (re-probe storesearch if the name changed), then a servable HEAD
     check on a source zip. Emit sorted, deduped unlisted.json.

Output stats vs the previous file (added / removed / renamed) are printed.

Usage:
  set GH_TOKEN, then:
  python tools/generate_unlisted_index.py --out unlisted.json
"""

import argparse
import json
import os
import random
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
from datetime import datetime, timezone

API = "https://api.github.com"
STORE = "https://store.steampowered.com"
MORRENUS = "https://applist.morrenus.xyz/"
UA = {"User-Agent": "gabluchi-search-generator/1.0"}

REPOS = {
    "mirror": "Gab-lutang/gabluchi-manifests",
    "sushi": "sushi-dev55-alt/sushitools-games-repo-alt",
}
ZIP_URL = {
    "mirror": "https://raw.githubusercontent.com/Gab-lutang/gabluchi-manifests/main/{id}.zip",
    "sushi": "https://raw.githubusercontent.com/sushi-dev55-alt/sushitools-games-repo-alt/refs/heads/main/{id}.zip",
}
EQHUB = "CreeperKing3532/EQhub"


def log(msg):
    print(msg, flush=True)


def http(url, headers=None, tries=4):
    """GET with retry: backoff on 429/5xx, raise on hard failure."""
    hdrs = dict(UA)
    if headers:
        hdrs.update(headers)
    last = None
    for attempt in range(tries):
        req = urllib.request.Request(url, headers=hdrs)
        try:
            with urllib.request.urlopen(req, timeout=30) as res:
                return res.read()
        except urllib.error.HTTPError as e:
            if e.code in (429, 500, 502, 503, 504) and attempt < tries - 1:
                retry = e.headers.get("Retry-After")
                wait = float(retry) if retry and retry.isdigit() else 2.0 ** (attempt + 1)
                time.sleep(wait)
                last = e
                continue
            raise
        except (urllib.error.URLError, TimeoutError) as e:
            last = e
            if attempt < tries - 1:
                time.sleep(2.0 ** (attempt + 1))
                continue
            raise
    raise last


def head_ok(url):
    req = urllib.request.Request(url, method="HEAD", headers=UA)
    try:
        with urllib.request.urlopen(req, timeout=20) as res:
            return 200 <= res.status < 300
    except urllib.error.HTTPError as e:
        return 200 <= e.code < 300
    except Exception:
        return False


def repo_zip_ids(repo, token):
    """Set of numeric appids that have a *.zip anywhere in the repo tree."""
    data = json.loads(http(f"{API}/repos/{repo}/git/trees/main?recursive=1",
                           {"Authorization": f"Bearer {token}"}))
    ids = set()
    for entry in data.get("tree", []):
        path = entry.get("path", "")
        if path.endswith(".zip"):
            stem = path.rsplit("/", 1)[-1][:-4]
            if stem.isdigit():
                ids.add(int(stem))
    if data.get("truncated"):
        log(f"  WARN: tree for {repo} was truncated ({len(ids)} ids kept)")
    return ids


def eqhub_branch_ids(token):
    """EQhub keeps one git branch per appid; GraphQL enumerates 62k fast."""
    ids = set()
    cursor = None
    while True:
        query = (
            'query{repository(owner:"CreeperKing3532",name:"EQhub"){'
            'refs(first:100,refPrefix:"refs/heads/"'
            + (f',after:"{cursor}"' if cursor else "")
            + '){nodes{name} pageInfo{hasNextPage endCursor}}}}'
        )
        body = json.dumps({"query": query}).encode()
        req = urllib.request.Request(f"{API}/graphql", data=body, method="POST",
                                     headers={"Authorization": f"Bearer {token}",
                                              "User-Agent": "gabluchi-search-generator/1.0",
                                              "Content-Type": "application/json"})
        data = json.loads(urllib.request.urlopen(req, timeout=30).read())
        if "errors" in data:
            raise RuntimeError(f"GraphQL: {data['errors']}")
        refs = data["data"]["repository"]["refs"]
        for node in refs["nodes"]:
            name = node["name"]
            if name.isdigit():
                ids.add(int(name))
        if not refs["pageInfo"]["hasNextPage"]:
            break
        cursor = refs["pageInfo"]["endCursor"]
    return ids


def morrenus_names():
    """appid -> name from the 380k morrenus applist (includes delisted)."""
    data = json.loads(http(MORRENUS, tries=3))
    return {int(x["appid"]): (x.get("name") or "").strip()
            for x in data if str(x.get("appid", "")).isdigit()}


def storesearch_has(appid, name, delay):
    """True when the appid appears in Steam storesearch for this name."""
    time.sleep(delay)
    url = f"{STORE}/api/storesearch/?term={urllib.parse.quote(name)}&l=english&cc=US"
    for attempt, wait in enumerate((0, 8, 20)):
        if wait:
            time.sleep(wait)
        try:
            body = json.loads(http(url, tries=2))
        except Exception as e:
            if attempt == 2:
                log(f"  storesearch {appid} failed ({e}); treating as listed")
                return True
            continue
        for item in body.get("items") or []:
            if item.get("id") == appid:
                return True
        return False
    return True


def appdetails(appid, delay):
    """(canonical name, type) or None when success=false / unreachable."""
    time.sleep(delay)
    url = f"{STORE}/api/appdetails?appids={appid}&filters=basic&l=english&cc=US"
    try:
        body = json.loads(http(url))
    except Exception as e:
        log(f"  appdetails {appid} error: {e}")
        return None
    entry = body.get(str(appid)) or {}
    if not entry.get("success"):
        return None
    data = entry.get("data") or {}
    name = (data.get("name") or "").strip()
    if not name:
        return None
    return name, (data.get("type") or "")


def load_probe_cache(path):
    """appid -> listed bool, from an append-only JSONL file (crash-safe)."""
    cache = {}
    if os.path.exists(path):
        with open(path, "r", encoding="utf-8") as f:
            for line in f:
                line = line.strip()
                if not line:
                    continue
                try:
                    rec = json.loads(line)
                    cache[int(rec["appid"])] = bool(rec["listed"])
                except Exception:
                    continue
    return cache


def load_previous(path):
    try:
        with open(path, "r", encoding="utf-8") as f:
            prev = json.load(f)
        return {int(g["appid"]): g["name"] for g in prev.get("games", [])}
    except Exception:
        return {}


def main():
    ap = argparse.ArgumentParser(description="Generate unlisted.json for gabluchi-search")
    ap.add_argument("--out", required=True, help="path to unlisted.json")
    ap.add_argument("--delay", type=float, default=0.65,
                    help="seconds between Steam store API calls")
    ap.add_argument("--limit", type=int, default=0,
                    help="probe at most N games (debug)")
    ap.add_argument("--eqhub", action="store_true",
                    help="include EQhub's 62k branches (hours of probes)")
    args = ap.parse_args()

    token = os.environ.get("GH_TOKEN") or os.environ.get("GITHUB_TOKEN")
    if not token:
        log("ERROR: set GH_TOKEN or GITHUB_TOKEN")
        return 1
    cache_path = args.out + ".probes.jsonl"

    log("[1/4] enumerating sources ...")
    mirror_ids = repo_zip_ids(REPOS["mirror"], token)
    log(f"  mirror: {len(mirror_ids)} zips")
    sushi_ids = repo_zip_ids(REPOS["sushi"], token)
    log(f"  sushi:  {len(sushi_ids)} zips")
    sources = {}
    for src, ids in (("mirror", mirror_ids), ("sushi", sushi_ids)):
        for i in ids:
            sources.setdefault(i, []).append(src)
    if args.eqhub:
        eq_ids = eqhub_branch_ids(token)
        log(f"  eqhub:  {len(eq_ids)} branches")
        for i in eq_ids:
            sources.setdefault(i, []).append("eqhub")
    universe = sorted(sources)
    log(f"  universe: {len(universe)} source appids")

    log("[2/4] loading morrenus names ...")
    names = morrenus_names()
    log(f"  names: {len(names)}")
    named = [(i, names[i]) for i in universe if names.get(i)]
    unnamed = [i for i in universe if not names.get(i)]
    log(f"  named: {len(named)}  unnamed(needs appdetails first): {len(unnamed)}")

    cache = load_probe_cache(cache_path)
    fresh = [t for t in named if t[0] not in cache]
    log(f"[3/4] storesearch probes: {len(fresh)} fresh, {len(named) - len(fresh)} cached")
    survivors = []
    probed = 0
    with open(cache_path, "a", encoding="utf-8") as cf:
        def probe(appid, name):
            listed = storesearch_has(appid, name, args.delay)
            cf.write(json.dumps({"appid": appid, "listed": listed,
                                 "ts": int(time.time())}) + "\n")
            cf.flush()
            cache[appid] = listed
            return listed

        for appid in unnamed:
            res = appdetails(appid, args.delay)
            if res is None:
                continue
            listed = probe(appid, res[0])
            if not listed and res[1] == "game":
                survivors.append((appid, res[0]))
        for appid, name in fresh:
            if not probe(appid, name):
                survivors.append((appid, name))
            probed += 1
            if probed % 100 == 0:
                log(f"  {probed}/{len(fresh)} probed (unlisted so far: {len(survivors)})")
            if args.limit and probed >= args.limit:
                log(f"  --limit {args.limit} reached, stopping probes")
                break
        # survivors from earlier cached probes
        if not args.limit:
            for appid, name in named:
                if not cache.get(appid, True) and (appid, name) not in survivors:
                    survivors.append((appid, name))
    log(f"  confirmed unlisted: {len(survivors)}")

    log("[4/4] canonical name + type=game + servable HEAD ...")
    final, dropped = [], []
    for n, (appid, name) in enumerate(survivors, 1):
        keep_name = name
        res = appdetails(appid, args.delay)
        if res is None:
            dropped.append((appid, "appdetails-dead"))
            continue
        canon, typ = res
        if typ != "game":
            dropped.append((appid, f"type={typ}"))
            continue
        if canon != name:
            # rename -> re-confirm with the canonical name
            if storesearch_has(appid, canon, args.delay):
                cache[appid] = True
                dropped.append((appid, "listed-after-rename"))
                continue
            keep_name = canon
        ok = False
        for src in sources[appid]:
            if src == "eqhub":
                ok = True
                break
            if head_ok(ZIP_URL[src].format(id=appid)):
                ok = True
                break
        if ok:
            final.append((appid, keep_name))
        else:
            dropped.append((appid, "zip-dead"))
        if n % 50 == 0:
            log(f"  {n}/{len(survivors)} finalized (kept {len(final)})")
    log(f"  kept: {len(final)}  dropped: {len(dropped)}")
    for appid, why in dropped[:20]:
        log(f"    drop {appid}: {why}")

    final.sort(key=lambda t: t[0])
    games_out = [{"appid": a, "name": n} for a, n in final]
    out = {
        "generated": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "count": len(games_out),
        "games": games_out,
    }
    with open(args.out, "w", encoding="utf-8", newline="\n") as f:
        json.dump(out, f, ensure_ascii=False, indent=1)
        f.write("\n")

    prev = load_previous(args.out)
    now = {g["appid"]: g["name"] for g in games_out}
    added = sorted(set(now) - set(prev))
    removed = sorted(set(prev) - set(now))
    renamed = sorted(a for a in set(now) & set(prev) if now[a] != prev[a])
    log("")
    log(f"WROTE {args.out}: {len(games_out)} games")
    log(f"  added:   {len(added)}" + (f" -> {added[:10]}{' ...' if len(added) > 10 else ''}" if added else ""))
    log(f"  removed: {len(removed)}" + (f" -> {removed[:10]}{' ...' if len(removed) > 10 else ''}" if removed else ""))
    log(f"  renamed: {len(renamed)}" + (f" -> {renamed[:10]}{' ...' if len(renamed) > 10 else ''}" if renamed else ""))
    for appid, name in final[:15]:
        log(f"    {appid}  {name}")
    if len(final) > 15:
        log(f"    ... and {len(final) - 15} more")
    return 0


if __name__ == "__main__":
    sys.exit(main())
