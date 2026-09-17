using System;
using System.Collections.Generic;
using System.Linq;
using GabLuchi.Models;

namespace GabLuchi.Services;

public static class GamePortLookup
{
	private static readonly List<GameInfo> Games = new List<GameInfo>
	{
		// Source Engine
		new GameInfo { Name = "Counter-Strike 1.6", AppId = 10, DefaultPort = 27015, PortNote = "GoldSrc" },
		new GameInfo { Name = "Team Fortress Classic", AppId = 20, DefaultPort = 27015, PortNote = "GoldSrc" },
		new GameInfo { Name = "Day of Defeat", AppId = 30, DefaultPort = 27015, PortNote = "GoldSrc" },
		new GameInfo { Name = "Half-Life", AppId = 70, DefaultPort = 27015, PortNote = "GoldSrc" },
		new GameInfo { Name = "Half-Life 2", AppId = 220, DefaultPort = 27015, PortNote = "Source" },
		new GameInfo { Name = "Counter-Strike: Source", AppId = 240, DefaultPort = 27015, PortNote = "Source" },
		new GameInfo { Name = "Day of Defeat: Source", AppId = 300, DefaultPort = 27015, PortNote = "Source" },
		new GameInfo { Name = "Team Fortress 2", AppId = 440, DefaultPort = 27015, PortNote = "Source" },
		new GameInfo { Name = "Left 4 Dead", AppId = 500, DefaultPort = 27015, PortNote = "Source" },
		new GameInfo { Name = "Left 4 Dead 2", AppId = 550, DefaultPort = 27015, PortNote = "Source" },
		new GameInfo { Name = "Portal 2", AppId = 620, DefaultPort = 27015, PortNote = "Source" },
		new GameInfo { Name = "Alien Swarm", AppId = 630, DefaultPort = 27015, PortNote = "Source" },
		new GameInfo { Name = "Counter-Strike 2", AppId = 730, DefaultPort = 27015, PortNote = "Source 2" },
		new GameInfo { Name = "Garry's Mod", AppId = 4000, DefaultPort = 27015, PortNote = "Source" },

		// Unreal Engine
		new GameInfo { Name = "Killing Floor", AppId = 1250, DefaultPort = 27015, PortNote = "UE2" },
		new GameInfo { Name = "Killing Floor 2", AppId = 232090, DefaultPort = 7777, PortNote = "UE3" },
		new GameInfo { Name = "Insurgency: Sandstorm", AppId = 354500, DefaultPort = 27015, PortNote = "UE4" },
		new GameInfo { Name = "Mordhau", AppId = 489630, DefaultPort = 7777, PortNote = "UE4" },
		new GameInfo { Name = "Chivalry 2", AppId = 1222700, DefaultPort = 7777, PortNote = "UE4" },
		new GameInfo { Name = "V Rising", AppId = 1829350, DefaultPort = 27015, PortNote = "Valve protocol" },
		new GameInfo { Name = "ARK: Survival Evolved", AppId = 346110, DefaultPort = 7777, PortNote = "UE4" },
		new GameInfo { Name = "Satisfactory", AppId = 526870, DefaultPort = 7777, PortNote = "UE4" },
		new GameInfo { Name = "Palworld", AppId = 1623730, DefaultPort = 8211, PortNote = "UE5" },
		new GameInfo { Name = "Enshrouded", AppId = 2278520, DefaultPort = 15636, PortNote = "UE5" },
		new GameInfo { Name = "SCUM", AppId = 513710, DefaultPort = 7042, PortNote = "UE4" },
		new GameInfo { Name = "Astroneer", AppId = 728470, DefaultPort = 8777, PortNote = "UE4" },
		new GameInfo { Name = "Craftopia", AppId = 1307550, DefaultPort = 6587, PortNote = "UE4" },
		new GameInfo { Name = "Soulmask", AppId = 3040080, DefaultPort = 7777, PortNote = "UE5" },
		new GameInfo { Name = "Longvinter", AppId = 1628750, DefaultPort = 7777, PortNote = "Unity" },
		new GameInfo { Name = "Last Oasis", AppId = 920720, DefaultPort = 5555, PortNote = "UE4" },
		new GameInfo { Name = "The Front", AppId = 2285150, DefaultPort = 25010, PortNote = "UE5" },
		new GameInfo { Name = "Rising World", AppId = 324080, DefaultPort = 4255, PortNote = "Unity" },

		// Unity / Multiplayer
		new GameInfo { Name = "Terraria", AppId = 105600, DefaultPort = 7777, PortNote = "TCP" },
		new GameInfo { Name = "7 Days to Die", AppId = 251570, DefaultPort = 26900, PortNote = "Unity" },
		new GameInfo { Name = "The Forest", AppId = 24554, DefaultPort = 27015, PortNote = "Unity" },
		new GameInfo { Name = "Sons of the Forest", AppId = 1326470, DefaultPort = 8766, PortNote = "Unity" },
		new GameInfo { Name = "Valheim", AppId = 896660, DefaultPort = 2456, PortNote = "Dedicated server" },
		new GameInfo { Name = "Rust", AppId = 252490, DefaultPort = 28015, PortNote = "Unity" },
		new GameInfo { Name = "Project Zomboid", AppId = 108600, DefaultPort = 16261, PortNote = "Unity" },
		new GameInfo { Name = "Don't Starve Together", AppId = 322330, DefaultPort = 10999, PortNote = "Klei" },
		new GameInfo { Name = "Factorio", AppId = 427520, DefaultPort = 34197, PortNote = "Custom" },
		new GameInfo { Name = "Starbound", AppId = 211820, DefaultPort = 21025, PortNote = "Custom" },
		new GameInfo { Name = "Core Keeper", AppId = 162100, DefaultPort = 27015, PortNote = "Unity" },
		new GameInfo { Name = "Colony Survival", AppId = 366090, DefaultPort = 27016, PortNote = "Unity" },
		new GameInfo { Name = "Stationeers", AppId = 544550, DefaultPort = 27500, PortNote = "Unity" },
		new GameInfo { Name = "Space Engineers", AppId = 280160, DefaultPort = 27016, PortNote = "Custom" },
		new GameInfo { Name = "Avorion", AppId = 565060, DefaultPort = 27000, PortNote = "Unity" },
		new GameInfo { Name = "Empyrion", AppId = 530870, DefaultPort = 30000, PortNote = "Unity" },
		new GameInfo { Name = "Wurm Unlimited", AppId = 366220, DefaultPort = 3724, PortNote = "Java" },
		new GameInfo { Name = "CryoFall", AppId = 829590, DefaultPort = 6000, PortNote = "Unity" },
		new GameInfo { Name = "Risk of Rain 2", AppId = 1180760, DefaultPort = 27015, PortNote = "Unity" },
		new GameInfo { Name = "Squad", AppId = 393380, DefaultPort = 27015, PortNote = "Unity" },
		new GameInfo { Name = "Natural Selection 2", AppId = 4940, DefaultPort = 27015, PortNote = "Unity" },
		new GameInfo { Name = "No More Room in Hell", AppId = 317670, DefaultPort = 27015, PortNote = "Unity" },
		new GameInfo { Name = "Tower Unite", AppId = 439660, DefaultPort = 27015, PortNote = "Unity" },
		new GameInfo { Name = "SCP: Secret Laboratory", AppId = 996560, DefaultPort = 7777, PortNote = "Unity" },

		// Party / Co-op
		new GameInfo { Name = "Gang Beasts", AppId = 285900, DefaultPort = 6000, PortNote = "Dedicated server" },
		new GameInfo { Name = "Phasmophobia", AppId = 739630, DefaultPort = 0, PortNote = "Photon relay" },
		new GameInfo { Name = "Content Warning", AppId = 2881650, DefaultPort = 0, PortNote = "Photon relay" },
		new GameInfo { Name = "R.E.P.O.", AppId = 3241660, DefaultPort = 0, PortNote = "Photon relay" },
		new GameInfo { Name = "Lethal Company", AppId = 1966720, DefaultPort = 7777, PortNote = "Steam SDR" },
		new GameInfo { Name = "Deep Rock Galactic", AppId = 548430, DefaultPort = 0, PortNote = "Dedicated relay" },
		new GameInfo { Name = "Stardew Valley", AppId = 413150, DefaultPort = 0, PortNote = "P2P via Steam" },

		// Shooter
		new GameInfo { Name = "Borderlands 3", AppId = 397540, DefaultPort = 0, PortNote = "Dedicated relay" },
		new GameInfo { Name = "Deep Rock Galactic", AppId = 548430, DefaultPort = 0, PortNote = "Dedicated relay" },
		new GameInfo { Name = "Warframe", AppId = 230410, DefaultPort = 0, PortNote = "Dedicated" },
		new GameInfo { Name = "Dead by Daylight", AppId = 381210, DefaultPort = 0, PortNote = "Dedicated" },
	};

	public static List<GameInfo> Search(string query)
	{
		if (string.IsNullOrWhiteSpace(query))
		{
			return Games.OrderBy(g => g.Name).ToList();
		}

		string q = query.Trim().ToLowerInvariant();

		return Games
			.Select(g => new { Game = g, Score = FuzzyScore(g.Name.ToLowerInvariant(), q) })
			.Where(x => x.Score > 0)
			.OrderByDescending(x => x.Score)
			.ThenBy(x => x.Game.Name)
			.Select(x => x.Game)
			.Take(15)
			.ToList();
	}

	public static GameInfo? FindByAppId(long appId)
	{
		return Games.FirstOrDefault(g => g.AppId == appId);
	}

	private static int FuzzyScore(string name, string query)
	{
		if (name.Contains(query))
		{
			int idx = name.IndexOf(query, StringComparison.Ordinal);
			return 1000 - idx;
		}

		string[] queryWords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		int matchCount = 0;
		int totalScore = 0;

		foreach (string word in queryWords)
		{
			if (name.Contains(word))
			{
				matchCount++;
				totalScore += 500;
			}
		}

		if (matchCount == queryWords.Length && matchCount > 0)
		{
			return totalScore;
		}

		int qIdx = 0;
		int consecutive = 0;
		int partialScore = 0;

		for (int i = 0; i < name.Length && qIdx < query.Length; i++)
		{
			if (name[i] == query[qIdx])
			{
				qIdx++;
				consecutive++;
				partialScore += consecutive * 10;
			}
			else
			{
				consecutive = 0;
			}
		}

		if (qIdx == query.Length)
		{
			return partialScore;
		}

		return 0;
	}
}
