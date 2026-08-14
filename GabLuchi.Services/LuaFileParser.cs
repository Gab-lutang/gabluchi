using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;

namespace GabLuchi.Services;

public static class LuaFileParser
{
	private static Regex AddAppIdRegex()
	{
		return _003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__AddAppIdRegex_3.Instance;
	}

	private static Regex SetManifestRegex()
	{
		return _003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__SetManifestRegex_4.Instance;
	}

	private static Regex CommentTailRegex()
	{
		return _003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__CommentTailRegex_5.Instance;
	}

	private static string? CleanComment(string? raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
		{
			return null;
		}
		string text = raw.Trim();
		if (text.Contains("setManifestid", StringComparison.OrdinalIgnoreCase) || text.Contains("addappid", StringComparison.OrdinalIgnoreCase) || text.Contains("addtoken", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		text = CommentTailRegex().Replace(text, "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return null;
	}

	public static LuaContents? Parse(string filePath, long appIdFromName)
	{
		try
		{
			string text = File.ReadAllText(filePath);
			Dictionary<long, string> manifests = new Dictionary<long, string>();
			foreach (Match item in SetManifestRegex().Matches(text))
			{
				if (long.TryParse(item.Groups[1].Value, out var result))
				{
					manifests[result] = item.Groups[2].Value;
				}
			}
			List<long> list = new List<long>();
			Dictionary<long, bool> hasKeyById = new Dictionary<long, bool>();
			Dictionary<long, string> commentById = new Dictionary<long, string>();
			string[] array = text.Split('\n');
			for (int i = 0; i < array.Length; i++)
			{
				string text2 = array[i].Trim();
				if (text2.StartsWith("--"))
				{
					continue;
				}
				Match match2 = AddAppIdRegex().Match(text2);
				if (match2.Success && long.TryParse(match2.Groups[1].Value, out var result2))
				{
					bool flag = match2.Groups[2].Success && !string.IsNullOrEmpty(match2.Groups[2].Value);
					if (hasKeyById.TryGetValue(result2, out var value))
					{
						hasKeyById[result2] = value || flag;
					}
					else
					{
						hasKeyById[result2] = flag;
						list.Add(result2);
					}
					string text3 = CleanComment(match2.Groups[3].Success ? match2.Groups[3].Value : null);
					if (text3 != null && (!commentById.TryGetValue(result2, out string value2) || text3.Length > value2.Length))
					{
						commentById[result2] = text3;
					}
				}
			}
			List<LuaEntry> list2 = list.Select((long id) => new LuaEntry(id, hasKeyById[id], manifests.TryGetValue(id, out string value3) ? value3 : null, commentById.TryGetValue(id, out string value4) ? value4 : null)).ToList();
			return new LuaContents((list2.Count > 0) ? list2[0].Id : appIdFromName, list2);
		}
		catch
		{
			return null;
		}
	}

	public static LuaDiff Diff(LuaContents? oldLua, LuaContents newLua)
	{
		HashSet<long> oldIds = oldLua?.Entries.Select((LuaEntry e) => e.Id).ToHashSet() ?? new HashSet<long>();
		HashSet<long> newIds = newLua.Entries.Select((LuaEntry e) => e.Id).ToHashSet();
		List<LuaEntry> added = newLua.Entries.Where((LuaEntry e) => !oldIds.Contains(e.Id)).ToList();
		List<LuaEntry> removed = oldLua?.Entries.Where((LuaEntry e) => !newIds.Contains(e.Id)).ToList() ?? new List<LuaEntry>();
		return new LuaDiff(added, removed);
	}
}
