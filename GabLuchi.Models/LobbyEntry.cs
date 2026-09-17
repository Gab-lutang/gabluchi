using System;
using System.Text.Json.Serialization;

namespace GabLuchi.Models;

public class LobbyEntry
{
	[JsonPropertyName("code")]
	public string Code { get; set; } = "";

	[JsonPropertyName("gameName")]
	public string GameName { get; set; } = "";

	[JsonPropertyName("appId")]
	public long AppId { get; set; }

	[JsonPropertyName("hostName")]
	public string HostName { get; set; } = "";

	[JsonPropertyName("ip")]
	public string Ip { get; set; } = "";

	[JsonPropertyName("port")]
	public int Port { get; set; }

	[JsonPropertyName("createdAt")]
	public DateTime CreatedAt { get; set; }

	public string AgeText
	{
		get
		{
			TimeSpan age = DateTime.UtcNow - CreatedAt;
			if (age.TotalMinutes < 1)
				return "just now";
			if (age.TotalMinutes < 60)
				return ((int)age.TotalMinutes) + "m";
			if (age.TotalHours < 24)
				return ((int)age.TotalHours) + "h";
			return ((int)age.TotalDays) + "d";
		}
	}

	public string ConnectText => Ip + ":" + Port;
}
