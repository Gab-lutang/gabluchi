using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;

namespace GabLuchi.Services;

public class SteamLibraryService(SteamService steam)
{
	private static Regex PathRegex()
	{
		return _003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__PathRegex_7.Instance;
	}

	private static Regex InstallDirRegex()
	{
		return _003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__InstallDirRegex_8.Instance;
	}

	public string? GetInstallDir(long appId)
	{
		try
		{
			string effectivePath = steam.EffectivePath;
			if (effectivePath == null)
			{
				return null;
			}
			foreach (string libraryRoot in GetLibraryRoots(effectivePath))
			{
				string path = Path.Combine(libraryRoot, "steamapps", $"appmanifest_{appId}.acf");
				if (!File.Exists(path))
				{
					continue;
				}
				Match match = InstallDirRegex().Match(File.ReadAllText(path));
				if (match.Success)
				{
					string path2 = Unescape(match.Groups[1].Value);
					string text = Path.Combine(libraryRoot, "steamapps", "common", path2);
					if (Directory.Exists(text))
					{
						return text;
					}
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static IEnumerable<string> GetLibraryRoots(string steamRoot)
	{
		yield return steamRoot;
		string path = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
		if (!File.Exists(path))
		{
			yield break;
		}
		string input;
		try
		{
			input = File.ReadAllText(path);
		}
		catch
		{
			yield break;
		}
		foreach (Match item in PathRegex().Matches(input))
		{
			string text = Unescape(item.Groups[1].Value);
			if (!string.Equals(text, steamRoot, StringComparison.OrdinalIgnoreCase) && Directory.Exists(text))
			{
				yield return text;
			}
		}
	}

	private static string Unescape(string s)
	{
		return s.Replace("\\\\", "\\");
	}

	public List<long> GetInstalledAppIds()
	{
		List<long> ids = new List<long>();
		try
		{
			string effectivePath = steam.EffectivePath;
			if (effectivePath == null)
				return ids;

			foreach (string root in GetLibraryRoots(effectivePath))
			{
				string steamappsDir = Path.Combine(root, "steamapps");
				if (!Directory.Exists(steamappsDir))
					continue;

				foreach (string acf in Directory.GetFiles(steamappsDir, "appmanifest_*.acf"))
				{
					string fileName = Path.GetFileNameWithoutExtension(acf);
					if (fileName.StartsWith("appmanifest_") && long.TryParse(fileName.Substring(12), out long appId))
					{
						ids.Add(appId);
					}
				}
			}
		}
		catch
		{
		}
		return ids;
	}
}
