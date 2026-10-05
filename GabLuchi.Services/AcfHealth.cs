using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace GabLuchi.Services;

public enum AcfState
{
	Missing,
	Healthy,
	Failed,
}

/// <summary>
/// Read-only verdict on a game's appmanifest across every Steam library. A keyed lua on disk is
/// not proof that the game installs: a failed download leaves a broken acf behind while the lua
/// happily passes the key check, so both the fetch gate and the pin writer ask this instead.
/// </summary>
public static class AcfHealth
{
	private static readonly Regex UpdateResultRegex = new("\"UpdateResult\"\\s+\"(-?\\d+)\"", RegexOptions.Compiled);
	private static readonly Regex StateFlagsRegex = new("\"StateFlags\"\\s+\"(\\d+)\"", RegexOptions.Compiled);
	private static readonly Regex SizeOnDiskRegex = new("\"SizeOnDisk\"\\s+\"(\\d+)\"", RegexOptions.Compiled);

	public static AcfState GetState(string? steamRoot, long appId)
	{
		if (string.IsNullOrEmpty(steamRoot))
		{
			return AcfState.Missing;
		}
		try
		{
			foreach (string root in SteamLibraryService.GetLibraryRoots(steamRoot))
			{
				string acf = Path.Combine(root, "steamapps", $"appmanifest_{appId}.acf");
				if (!File.Exists(acf))
				{
					continue;
				}
				return ParseState(acf);
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"[AcfHealth] GetState({appId}) failed: {ex.Message}");
		}
		return AcfState.Missing;
	}

	private static AcfState ParseState(string acfPath)
	{
		string text;
		try
		{
			text = File.ReadAllText(acfPath);
		}
		catch
		{
			return AcfState.Missing;
		}
		long updateResult = MatchLong(UpdateResultRegex, text);
		long stateFlags = MatchLong(StateFlagsRegex, text);
		long sizeOnDisk = MatchLong(SizeOnDiskRegex, text);
		if (updateResult != 0 || ((stateFlags & 2) != 0 && sizeOnDisk == 0))
		{
			return AcfState.Failed;
		}
		if (sizeOnDisk > 0)
		{
			return AcfState.Healthy;
		}
		return AcfState.Missing;
	}

	private static long MatchLong(Regex regex, string text)
	{
		Match match = regex.Match(text);
		return match.Success && long.TryParse(match.Groups[1].Value, out long value) ? value : 0;
	}
}
