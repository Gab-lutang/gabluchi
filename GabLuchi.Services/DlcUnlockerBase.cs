using System.Collections.Generic;
using GabLuchi.Models;

namespace GabLuchi.Services;

public abstract class DlcUnlockerBase
{
	public abstract DlcUnlockerType Type { get; }

	public abstract string DisplayName { get; }

	public virtual bool IsAvailable() => true;

	public abstract bool IsInstalled(string gameDir);

	public abstract DlcUnlockerInstallResult Install(string gameDir, List<long> dlcIds, long appId);

	public abstract bool Uninstall(string gameDir);

	public abstract string[] ConfigFileNames { get; }

	public abstract DlcUnlockerType[] ConflictsWith { get; }
}
