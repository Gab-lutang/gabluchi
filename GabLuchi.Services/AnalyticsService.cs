using System;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GabLuchi.Services;

public class AnalyticsService
{
	private readonly HttpClient _http = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(10.0)
	};

	private static readonly string Version;

	public async Task TrackAppLaunchAsync(CancellationToken ct = default(CancellationToken))
	{
		try
		{
			var value = new
			{
				type = "event",
				payload = new
				{
					website = "820d782c-a434-424f-9f90-dee83dc6032e",
					hostname = "desktop.lua.tools",
					url = "/launch/" + Version,
					title = "App Launch"
				}
			};
			using HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, "https://analytics.lua.tools/api/send")
			{
				Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json")
			};
			req.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
			await _http.SendAsync(req, ct);
		}
		catch
		{
		}
	}

	static AnalyticsService()
	{
		string text = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
		object version;
		if (text != null)
		{
			int num = text.IndexOf('+');
			if (num >= 0)
			{
				version = text.Substring(0, num);
				goto IL_0055;
			}
		}
		version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "?";
		goto IL_0055;
		IL_0055:
		Version = (string)version;
	}
}
