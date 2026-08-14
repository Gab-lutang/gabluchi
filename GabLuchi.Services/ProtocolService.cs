using System;
using System.IO;
using Microsoft.Win32;

namespace GabLuchi.Services;

public static class ProtocolService
{
	private const string ProtocolName = "gabluchi";

	private static readonly string PendingFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GabLuchi", "protocol_url.tmp");

	public static void Register()
	{
		try
		{
			string text = Environment.ProcessPath ?? "";
			using RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Software\\Classes\\gabluchi\\shell\\open\\command");
			registryKey.SetValue("", "\"" + text + "\" \"%1\"");
			using RegistryKey registryKey2 = Registry.CurrentUser.CreateSubKey("Software\\Classes\\gabluchi");
			registryKey2.SetValue("", "URL:GabLuchi Protocol");
			registryKey2.SetValue("URL Protocol", "");
		}
		catch
		{
		}
	}

	public static (string? Action, long? AppId, bool Silent) Parse(string url)
	{
		if (string.IsNullOrWhiteSpace(url))
		{
			return (Action: null, AppId: null, Silent: false);
		}
		Uri uri;
		try
		{
			uri = new Uri(url);
		}
		catch
		{
			return (Action: null, AppId: null, Silent: false);
		}
		if (!uri.Scheme.Equals("gabluchi", StringComparison.OrdinalIgnoreCase))
		{
			return (Action: null, AppId: null, Silent: false);
		}
		string text = uri.Authority.ToLowerInvariant();
		bool flag;
		switch (text)
		{
		case "game":
		case "install":
		case "manage":
		case "fix":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (!flag)
		{
			return (Action: null, AppId: null, Silent: false);
		}
		string text2 = uri.AbsolutePath.TrimStart('/');
		bool item = false;
		if (text == "install" && text2.StartsWith("silent/", StringComparison.OrdinalIgnoreCase))
		{
			item = true;
			text2 = text2.Substring("silent/".Length);
		}
		if (!long.TryParse(text2, out var result))
		{
			return (Action: null, AppId: null, Silent: false);
		}
		return (Action: text, AppId: result, Silent: item);
	}

	public static void WritePending(string url)
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(PendingFile));
			File.WriteAllText(PendingFile, url);
		}
		catch
		{
		}
	}

	public static string? TryReadPending()
	{
		try
		{
			if (File.Exists(PendingFile))
			{
				string text = File.ReadAllText(PendingFile).Trim();
				File.Delete(PendingFile);
				return string.IsNullOrEmpty(text) ? null : text;
			}
		}
		catch
		{
		}
		return null;
	}
}
