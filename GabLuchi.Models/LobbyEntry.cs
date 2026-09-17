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
	[JsonConverter(typeof(FlexibleDateTimeConverter))]
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	public string AgeText
	{
		get
		{
			TimeSpan age = DateTime.UtcNow - CreatedAt;
			if (age.TotalSeconds < 0) return "just now";
			if (age.TotalMinutes < 1) return "just now";
			if (age.TotalMinutes < 60) return ((int)age.TotalMinutes) + "m";
			if (age.TotalHours < 24) return ((int)age.TotalHours) + "h";
			return ((int)age.TotalDays) + "d";
		}
	}

	public string ConnectText => Ip + ":" + Port;
}

public class FlexibleDateTimeConverter : JsonConverter<DateTime>
{
	public override DateTime Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
	{
		if (reader.TokenType == System.Text.Json.JsonTokenType.String)
		{
			string? str = reader.GetString();
			if (!string.IsNullOrEmpty(str) && DateTime.TryParse(str, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime dt))
			{
				return dt;
			}
		}
		if (reader.TokenType == System.Text.Json.JsonTokenType.Null)
		{
			return DateTime.UtcNow;
		}
		return DateTime.UtcNow;
	}

	public override void Write(System.Text.Json.Utf8JsonWriter writer, DateTime value, System.Text.Json.JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.ToString("o"));
	}
}
