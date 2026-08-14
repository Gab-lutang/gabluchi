using System;
using System.Collections.Generic;

namespace GabLuchi.Services;

public record InstallResult(bool LuaInstalled, int ManifestCount, IReadOnlyList<string> Failed, string? Error)
{
	public bool AnyFailed
	{
		get
		{
			if (Failed.Count <= 0)
			{
				return Error != null;
			}
			return true;
		}
	}

	public static InstallResult Fail(string error)
	{
		return new InstallResult(LuaInstalled: false, 0, Array.Empty<string>(), error);
	}
}
