using System;
using System.IO;

namespace GabLuchi.Services;

public class DemolishService(SteamService steam, LuaInstaller lua, SteamLibraryService library)
{
	public void DeleteAppPermanently(long appId)
	{
		lua.DeleteLua(appId);
		lua.DeleteManifestsForApp(appId);
		string? installDir = library.GetInstallDir(appId);
		if (installDir != null && Directory.Exists(installDir))
		{
			try { Directory.Delete(installDir, recursive: true); } catch { }
		}
		foreach (string root in library.GetLibraryRootsList())
		{
			string acf = Path.Combine(root, "steamapps", $"appmanifest_{appId}.acf");
			if (File.Exists(acf))
			{
				try { File.Delete(acf); } catch { }
			}
		}
	}

	public void DeleteAllGamesPermanently()
	{
		string? luaDir = steam.LuaDir;
		if (luaDir != null && Directory.Exists(luaDir))
		{
			foreach (string file in Directory.GetFiles(luaDir, "*.lua"))
			{
				long? appId = LuaInstaller.AppIdFromFileName(file);
				if (appId.HasValue)
				{
					DeleteAppPermanently(appId.Value);
				}
			}
		}
		lua.DeleteDllFiles();
	}
}