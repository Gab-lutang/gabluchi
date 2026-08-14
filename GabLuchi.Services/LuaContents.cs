using System.Collections.Generic;
using System.Linq;

namespace GabLuchi.Services;

public record LuaContents(long BaseAppId, IReadOnlyList<LuaEntry> Entries)
{
	public int DepotCount => Entries.Count((LuaEntry e) => e.HasKey);

	public int DlcCount => Entries.Count((LuaEntry e) => !e.HasKey && e.Id != BaseAppId);
}
