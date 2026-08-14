using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;
using System.Threading;
using System.Threading.Tasks;

namespace GabLuchi.Services;

public class DonateKeysService(SettingsService settings, SteamService steam, CacheService cache)
{
	private const string DonationUrl = "http://167.235.229.108/donatekeys/send";

	private readonly HttpClient _http = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(15.0)
	};

	private static Regex AppIdRegex()
	{
		return _003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__AppIdRegex_0.Instance;
	}

	private static Regex KeyRegex()
	{
		return _003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__KeyRegex_1.Instance;
	}

	public async Task SendPendingKeysIfEnabledAsync(CancellationToken ct = default(CancellationToken))
	{
		try
		{
			if (!settings.DonateKeys)
			{
				return;
			}
			string effectivePath = steam.EffectivePath;
			if (effectivePath == null)
			{
				return;
			}
			string path = Path.Combine(effectivePath, "config", "config.vdf");
			if (!File.Exists(path))
			{
				return;
			}
			List<(string, string)> list = ExtractKeys(File.ReadAllText(path));
			if (list.Count == 0)
			{
				return;
			}
			HashSet<string> donated = new HashSet<string>(cache.GetDonatedAppIds());
			List<(string appid, string key)> fresh = list.Where<(string, string)>(((string appid, string key) p) => !donated.Contains(p.appid)).ToList();
			if (fresh.Count == 0)
			{
				return;
			}
			string content = string.Join(",", fresh.Select(((string appid, string key) p) => p.appid + ":" + p.key));
			using HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, "http://167.235.229.108/donatekeys/send")
			{
				Content = new StringContent(content, Encoding.UTF8, "text/plain")
			};
			req.Headers.TryAddWithoutValidation("User-Agent", "GabLuchi");
			if ((await _http.SendAsync(req, ct)).StatusCode != HttpStatusCode.OK)
			{
				return;
			}
			donated.UnionWith(fresh.Select(((string appid, string key) p) => p.appid));
			cache.SaveDonatedAppIds(donated);
		}
		catch
		{
		}
	}

	private static List<(string appid, string key)> ExtractKeys(string content)
	{
		Dictionary<string, object> node = ParseVdf(content);
		List<(string, string)> result = new List<(string, string)>();
		Walk(node);
		return result;
		void Walk(Dictionary<string, object> dictionary)
		{
			foreach (var (text2, obj2) in dictionary)
			{
				if (obj2 is Dictionary<string, object> dictionary2)
				{
					if (dictionary2.TryGetValue("DecryptionKey", out var value) && value is string text3)
					{
						if (IsValidPair(text2, text3))
						{
							result.Add((text2, text3));
						}
					}
					else
					{
						Walk(dictionary2);
					}
				}
			}
		}
	}

	private static bool IsValidPair(string appid, string key)
	{
		if (AppIdRegex().IsMatch(appid))
		{
			return KeyRegex().IsMatch(key);
		}
		return false;
	}

	private static Dictionary<string, object> ParseVdf(string content)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		Stack<Dictionary<string, object>> stack = new Stack<Dictionary<string, object>>();
		stack.Push(dictionary);
		string text = null;
		int num = 0;
		int length = content.Length;
		while (num < length)
		{
			char c = content[num];
			if (c == '/' && num + 1 < length && content[num + 1] == '/')
			{
				int num2 = content.IndexOf('\n', num);
				num = ((num2 < 0) ? length : (num2 + 1));
				continue;
			}
			switch (c)
			{
			case '"':
				break;
			case '{':
				if (text != null)
				{
					Dictionary<string, object> dictionary2 = new Dictionary<string, object>();
					stack.Peek()[text] = dictionary2;
					stack.Push(dictionary2);
					text = null;
				}
				num++;
				continue;
			case '}':
				if (stack.Count > 1)
				{
					stack.Pop();
				}
				num++;
				continue;
			default:
				num++;
				continue;
			}
			int num3 = content.IndexOf('"', num + 1);
			if (num3 < 0)
			{
				break;
			}
			int num4 = num + 1;
			string text2 = content.Substring(num4, num3 - num4);
			num = num3 + 1;
			if (text == null)
			{
				text = text2;
				continue;
			}
			stack.Peek()[text] = text2;
			text = null;
		}
		return dictionary;
	}
}
