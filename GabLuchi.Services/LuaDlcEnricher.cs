using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace GabLuchi.Services;

/// <summary>
/// Adds the DLC ids a lua cannot express on its own.
///
/// A depot key is what lets a lua mount an actual depot of content. Cosmetic DLC, soundtracks and
/// in-game currency have no depot of their own, so no keyed <c>addappid</c> line can ever cover
/// them and the unlocker never marks them owned. Steam, however, lists every DLC id a base game
/// declares in <c>extended.listofdlc</c>, and a bare <c>addappid(id)</c> line is enough for the
/// unlocker to treat that id as owned.
///
/// This enriches a staged lua with those lines after it is fetched. It is additive and idempotent:
/// an id already present as a keyed depot line, as a plain line, or repeated in a previous run is
/// never written twice, and a failure leaves the original file untouched.
/// </summary>
public sealed class LuaDlcEnricher(SteamDepotInfo depotInfo)
{
	private static readonly Regex PlainAddAppIdRegex = new(
		@"addappid\s*\(\s*(\d+)\s*\)\s*(?:--[ \t]*(.*))?$",
		RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex KeyedAddAppIdRegex = new(
		@"addappid\s*\(\s*(\d+)\s*,\s*[01]\s*,",
		RegexOptions.IgnoreCase | RegexOptions.Compiled);

	public async Task<int> EnrichLuaFileAsync(string luaPath, long appId, CancellationToken ct = default)
	{
		if (!File.Exists(luaPath))
		{
			return 0;
		}
		try
		{
			AppDepotInfo? info = await depotInfo.GetAsync(appId, ct).ConfigureAwait(false);
			IReadOnlyList<long> dlcIds = info?.DlcIds ?? Array.Empty<long>();
			return AppendDepotlessDlcIds(luaPath, appId, dlcIds);
		}
		catch
		{
			// Enrichment is a nicety. A lua that already carries its depot keys still unlocks the
			// game, so any failure here must leave the file as it was rather than block the install.
			return 0;
		}
	}

	public async Task<int> EnrichDownloadedFileAsync(string path, long appId, CancellationToken ct = default)
	{
		if (!File.Exists(path))
		{
			return 0;
		}
		if (!LooksLikeZip(path))
		{
			return await EnrichLuaFileAsync(path, appId, ct).ConfigureAwait(false);
		}
		try
		{
			AppDepotInfo? info = await depotInfo.GetAsync(appId, ct).ConfigureAwait(false);
			IReadOnlyList<long> dlcIds = info?.DlcIds ?? Array.Empty<long>();
			return AppendDepotlessDlcIdsToZip(path, appId, dlcIds);
		}
		catch
		{
			return 0;
		}
	}

	/// <summary>
	/// Appends one bare <c>addappid(id)</c> line per depotless DLC id that is not already present.
	/// Returns how many lines were added.
	/// </summary>
	public static int AppendDepotlessDlcIds(string luaPath, long appId, IReadOnlyList<long> dlcIds)
	{
		if (dlcIds.Count == 0 || !File.Exists(luaPath))
		{
			return 0;
		}
		string original;
		try
		{
			original = File.ReadAllText(luaPath);
		}
		catch
		{
			return 0;
		}
		string updated = AppendDepotlessDlcIdsToText(original, appId, dlcIds);
		if (string.Equals(original, updated, StringComparison.Ordinal))
		{
			return 0;
		}
		try
		{
			File.WriteAllText(luaPath, updated, new System.Text.UTF8Encoding(false));
			return 1;
		}
		catch
		{
			return 0;
		}
	}

	/// <summary>
	/// Pure text transform, exposed for tests. Returns the original string when nothing needs adding.
	/// </summary>
	public static string AppendDepotlessDlcIdsToText(string lua, long appId, IReadOnlyList<long> dlcIds)
	{
		if (string.IsNullOrEmpty(lua) || dlcIds.Count == 0)
		{
			return lua;
		}
		HashSet<long> present = new();
		foreach (string raw in lua.Split('\n'))
		{
			string line = raw.Trim();
			if (line.Length == 0)
			{
				continue;
			}
			// A commented-out line is inert: it does not register anything, so an id that only
			// appears commented must still be written live.
			if (line.StartsWith("--", StringComparison.Ordinal))
			{
				continue;
			}
			Match plain = PlainAddAppIdRegex.Match(line);
			if (plain.Success && long.TryParse(plain.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long plainId))
			{
				present.Add(plainId);
				continue;
			}
			Match keyed = KeyedAddAppIdRegex.Match(line);
			if (keyed.Success && long.TryParse(keyed.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long keyedId))
			{
				present.Add(keyedId);
			}
		}

		List<string> additions = new();
		HashSet<long> depotIds = new();
		foreach (Match m in KeyedAddAppIdRegex.Matches(lua))
		{
			if (long.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long id))
			{
				depotIds.Add(id);
			}
		}
		foreach (long dlcId in dlcIds)
		{
			if (dlcId == appId || present.Contains(dlcId) || depotIds.Contains(dlcId))
			{
				continue;
			}
			present.Add(dlcId);
			additions.Add($"addappid({dlcId.ToString(CultureInfo.InvariantCulture)})");
		}
		if (additions.Count == 0)
		{
			return lua;
		}

		string body = lua.EndsWith("\n", StringComparison.Ordinal) ? lua : lua + "\n";
		body += "\n-- DLC without a depot of its own (cosmetic/soundtrack/in-game currency).\n";
		body += "-- A keyed addappid cannot cover these, so they are registered bare.\n";
		body += string.Join("\n", additions) + "\n";
		return body;
	}

	/// <summary>
	/// Rewrites the lua entry inside a zip. The entry is re-added uncompressed so the shared cache
	/// validation, which scans stored bytes, still recognises the depot keys after enrichment.
	/// </summary>
	private static int AppendDepotlessDlcIdsToZip(string zipPath, long appId, IReadOnlyList<long> dlcIds)
	{
		if (dlcIds.Count == 0)
		{
			return 0;
		}
		string entryName;
		string luaText;
		byte[] originalEntryBytes;
		try
		{
			using ZipArchive archive = ZipFile.OpenRead(zipPath);
			ZipArchiveEntry? entry = archive.Entries.FirstOrDefault(e =>
				e.Name.EndsWith(".lua", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(e.Name));
			if (entry == null)
			{
				return 0;
			}
			entryName = entry.FullName;
			using Stream s = entry.Open();
			using MemoryStream ms = new();
			s.CopyTo(ms);
			originalEntryBytes = ms.ToArray();
			luaText = System.Text.Encoding.UTF8.GetString(originalEntryBytes);
		}
		catch
		{
			return 0;
		}

		string updated = AppendDepotlessDlcIdsToText(luaText, appId, dlcIds);
		if (string.Equals(updated, luaText, StringComparison.Ordinal))
		{
			return 0;
		}
		byte[] newBytes = System.Text.Encoding.UTF8.GetBytes(updated);

		string staging = zipPath + ".dlc.tmp";
		try
		{
			File.Copy(zipPath, staging, overwrite: true);
			using (ZipArchive archive = ZipFile.Open(staging, ZipArchiveMode.Update))
			{
				ZipArchiveEntry? existing = archive.GetEntry(entryName);
				if (existing != null)
				{
					existing.Delete();
				}
				ZipArchiveEntry fresh = archive.CreateEntry(entryName, CompressionLevel.NoCompression);
				using Stream outStream = fresh.Open();
				outStream.Write(newBytes, 0, newBytes.Length);
			}
			File.Move(staging, zipPath, overwrite: true);
			_ = originalEntryBytes;
			return newBytes.Length - originalEntryBytes.Length > 0 ? 1 : 0;
		}
		catch
		{
			try { File.Delete(staging); } catch { }
			return 0;
		}
	}

	private static bool LooksLikeZip(string path)
	{
		try
		{
			using FileStream stream = File.OpenRead(path);
			Span<byte> buffer = stackalloc byte[4];
			return stream.Read(buffer) == 4 && buffer[0] == 80 && buffer[1] == 75 && buffer[2] == 3 && buffer[3] == 4;
		}
		catch
		{
			return false;
		}
	}
}
