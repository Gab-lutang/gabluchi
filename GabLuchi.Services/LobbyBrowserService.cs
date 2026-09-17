using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GabLuchi.Services;

public class LobbyBrowserService
{
	private const string LobbiesUrl = "https://gabluchi-connect.onrender.com/lobbies";

	private static readonly HttpClient Http = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(8)
	};

	private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true
	};

	public async Task<List<Models.LobbyEntry>> FetchLobbiesAsync(CancellationToken ct = default)
	{
		try
		{
			string json = await Http.GetStringAsync(LobbiesUrl, ct);
			List<Models.LobbyEntry>? lobbies = JsonSerializer.Deserialize<List<Models.LobbyEntry>>(json, JsonOpts);
			return lobbies ?? new List<Models.LobbyEntry>();
		}
		catch
		{
			return new List<Models.LobbyEntry>();
		}
	}
}
