using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GabLuchi.Services;

public class SteamOwnershipService
{
	private static readonly HttpClient _http = new HttpClient(new HttpClientHandler
	{
		AutomaticDecompression = System.Net.DecompressionMethods.All
	})
	{
		Timeout = TimeSpan.FromSeconds(15)
	};

	public async Task<HashSet<long>> GetOwnedAppIdsAsync(string steamId64)
	{
		HashSet<long> owned = new HashSet<long>();
		if (string.IsNullOrWhiteSpace(steamId64))
		{
			return owned;
		}

		try
		{
			using HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, $"https://steamcommunity.com/profiles/{steamId64.Trim()}/games?tab=all&l=english");
			req.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Safari/537.36");
			using HttpResponseMessage res = await _http.SendAsync(req);
			if (!res.IsSuccessStatusCode)
			{
				return owned;
			}

			string html = await res.Content.ReadAsStringAsync();
			foreach (Match m in Regex.Matches(html, "\"appid\"\\s*:\\s*(\\d+)"))
			{
				if (long.TryParse(m.Groups[1].Value, out long appId))
				{
					owned.Add(appId);
				}
			}
		}
		catch
		{
			owned = new HashSet<long>();
		}

		return owned;
	}
}