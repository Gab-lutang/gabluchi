using System;
using System.IO;

namespace GabLuchi.Services;

public static class PluginLog
{
	private static readonly object _lock = new object();

	public static readonly string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GabLuchi", "plugin-backend.log");

	public static void Log(string msg)
	{
		try
		{
			lock (_lock)
			{
				Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
				File.AppendAllText(FilePath, $"[{DateTime.Now:HH:mm:ss.fff}] {msg}{Environment.NewLine}");
			}
		}
		catch
		{
		}
	}
}
