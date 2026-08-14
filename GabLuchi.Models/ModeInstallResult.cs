using System;
using System.Collections.Generic;

namespace GabLuchi.Models;

public sealed record ModeInstallResult(bool Success, string? Error, IReadOnlyList<string> Failed)
{
	public static ModeInstallResult Ok()
	{
		return new ModeInstallResult(Success: true, null, Array.Empty<string>());
	}

	public static ModeInstallResult Fail(string error)
	{
		return new ModeInstallResult(Success: false, error, Array.Empty<string>());
	}
}
