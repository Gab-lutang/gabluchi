using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GabLuchi.Models;

namespace GabLuchi.Services;

public class MultiplayerFixService
{
	private static readonly HttpClient Http = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(15)
	};

	private const string SearchUrl = "https://online-fix.me/index.php?do=search&subaction=search&story={0}";

	private const string GoogleSearchUrl = "https://www.google.com/search?q=site%3Aonline-fix.me+{0}";

	private static readonly Regex GameLinkRegex = new Regex("<a[^>]+href=\"(https?://online-fix\\.me/games/[^\"]+)\"[^>]*>(.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex TitleAttrRegex = new Regex("title=\"([^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex GoogleResultRegex = new Regex("<a[^>]+href=\"(https?://online-fix\\.me/[^\"]+)\"[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex TagStripRegex = new Regex("<[^>]+>", RegexOptions.Compiled);

	public async Task<List<MultiplayerFixResult>> SearchAsync(string gameName, CancellationToken ct = default)
	{
		List<MultiplayerFixResult> results = await SearchOnlineFixAsync(gameName, ct);
		if (results.Count >= 2)
		{
			return RankResults(gameName, results);
		}
		List<MultiplayerFixResult> googleResults = await SearchGoogleAsync(gameName, ct);
		results.AddRange(googleResults);
		return RankResults(gameName, results);
	}

	private async Task<List<MultiplayerFixResult>> SearchOnlineFixAsync(string gameName, CancellationToken ct)
	{
		string url = string.Format(SearchUrl, Uri.EscapeDataString(gameName));
		return await FetchAndParseAsync(url, ct);
	}

	private async Task<List<MultiplayerFixResult>> SearchGoogleAsync(string gameName, CancellationToken ct)
	{
		string url = string.Format(GoogleSearchUrl, Uri.EscapeDataString(gameName));
		try
		{
			HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, url);
			req.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
			HttpResponseMessage res = await Http.SendAsync(req, ct);
			if (!res.IsSuccessStatusCode)
			{
				return new List<MultiplayerFixResult>();
			}
			string html = await res.Content.ReadAsStringAsync(ct);
			return ParseGoogleResults(html);
		}
		catch
		{
			return new List<MultiplayerFixResult>();
		}
	}

	private async Task<List<MultiplayerFixResult>> FetchAndParseAsync(string url, CancellationToken ct)
	{
		HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, url);
		req.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
		HttpResponseMessage res = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
		res.EnsureSuccessStatusCode();
		byte[] bytes = await res.Content.ReadAsByteArrayAsync(ct);
		string html = Encoding.Latin1.GetString(bytes);
		return ParseSearchResults(html);
	}

	private static List<MultiplayerFixResult> ParseSearchResults(string html)
	{
		List<MultiplayerFixResult> results = new List<MultiplayerFixResult>();
		MatchCollection matches = GameLinkRegex.Matches(html);
		foreach (Match match in matches)
		{
			string href = match.Groups[1].Value;
			if (string.IsNullOrEmpty(href))
			{
				continue;
			}
			string title = "";
			Match titleAttr = TitleAttrRegex.Match(match.Value);
			if (titleAttr.Success)
			{
				title = System.Net.WebUtility.HtmlDecode(titleAttr.Groups[1].Value);
			}
			if (string.IsNullOrEmpty(title))
			{
				string innerHtml = match.Groups[2].Value;
				title = TagStripRegex.Replace(innerHtml, "").Trim();
			}
			if (string.IsNullOrEmpty(title))
			{
				title = ExtractTitleFromHref(href);
			}
			results.Add(new MultiplayerFixResult(href, title, 0.0));
		}
		return results.DistinctBy(r => r.Url).ToList();
	}

	private static List<MultiplayerFixResult> ParseGoogleResults(string html)
	{
		List<MultiplayerFixResult> results = new List<MultiplayerFixResult>();
		MatchCollection matches = GoogleResultRegex.Matches(html);
		foreach (Match match in matches)
		{
			string href = match.Groups[1].Value;
			if (string.IsNullOrEmpty(href) || href.Contains("google.com"))
			{
				continue;
			}
			string title = ExtractTitleFromHref(href);
			results.Add(new MultiplayerFixResult(href, title, 0.0));
		}
		return results.DistinctBy(r => r.Url).ToList();
	}

	private static string ExtractTitleFromHref(string href)
	{
		Uri? uri;
		if (!Uri.TryCreate(href, UriKind.Absolute, out uri))
		{
			return href;
		}
		string lastSegment = uri.Segments.Last().TrimEnd('/');
		if (!string.IsNullOrEmpty(lastSegment))
		{
			return Uri.UnescapeDataString(lastSegment).Replace('-', ' ').Replace('_', ' ');
		}
		return href;
	}

	private static List<MultiplayerFixResult> RankResults(string gameName, List<MultiplayerFixResult> results)
	{
		foreach (MultiplayerFixResult result in results)
		{
			result.Score = ScoreResult(gameName, result.Title, result.Url);
		}
		return results.OrderByDescending(r => r.Score).ToList();
	}

	private static double ScoreResult(string gameName, string title, string href)
	{
		string normalisedGame = NormalizeForMatch(gameName);
		string normalisedTitle = NormalizeForMatch(title);
		string normalisedHref = NormalizeForMatch(href);
		string[] gameTokens = normalisedGame.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		string combined = normalisedTitle + " " + normalisedHref;
		int matched = 0;
		foreach (string token in gameTokens)
		{
			if (combined.Contains(token))
			{
				matched++;
			}
		}
		double tokenCoverage = gameTokens.Length > 0 ? (double)matched / gameTokens.Length : 0.0;
		double bigramRatio = BigramRatio(normalisedGame, normalisedTitle);
		return tokenCoverage * 0.6 + bigramRatio * 0.4;
	}

	private static double BigramRatio(string a, string b)
	{
		if (a.Length < 2 || b.Length < 2)
		{
			return 0.0;
		}
		HashSet<string> aBigrams = GetBigrams(a);
		HashSet<string> bBigrams = GetBigrams(b);
		if (aBigrams.Count == 0 || bBigrams.Count == 0)
		{
			return 0.0;
		}
		int intersection = aBigrams.Intersect(bBigrams).Count();
		int union = aBigrams.Union(bBigrams).Count();
		return union > 0 ? (double)intersection / union : 0.0;
	}

	private static HashSet<string> GetBigrams(string s)
	{
		HashSet<string> bigrams = new HashSet<string>();
		for (int i = 0; i <= s.Length - 2; i++)
		{
			bigrams.Add(s.Substring(i, 2));
		}
		return bigrams;
	}

	private static string NormalizeForMatch(string s)
	{
		return s.ToLowerInvariant()
			.Replace(":", " ")
			.Replace(".", " ")
			.Replace(",", " ")
			.Replace("'", " ")
			.Replace("\"", " ")
			.Replace("-", " ")
			.Replace("_", " ")
			.Replace("  ", " ")
			.Trim();
	}

	public static void OpenInBrowser(string url)
	{
		try
		{
			Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
		}
		catch
		{
		}
	}
}
