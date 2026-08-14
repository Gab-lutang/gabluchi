using System;
using System.IO;
using GabLuchi;

namespace GabLuchi.Services;

public class FixRepository
{
	public static string DefaultRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GabLuchi", "fixes");

	public static string Root => (Config.FixRepositoryPath?.Trim().Length > 0) ? Config.FixRepositoryPath.Trim() : DefaultRoot;

	public static string? Resolve(string appid, string slot)
	{
		string fileName = (slot == "manifest") ? "manifest.zip" : "fix.zip";
		string path = Path.Combine(Root, appid, fileName);
		if (!File.Exists(path))
		{
			return null;
		}
		return path;
	}
}
