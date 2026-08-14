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

	public Task TrackAppLaunchAsync(CancellationToken ct = default(CancellationToken))
	{
		return Task.CompletedTask;
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
