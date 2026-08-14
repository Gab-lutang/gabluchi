using System.Collections.Generic;

namespace GabLuchi.Services;

public record LuaDiff(IReadOnlyList<LuaEntry> Added, IReadOnlyList<LuaEntry> Removed)
{
	public bool HasChanges
	{
		get
		{
			if (Added.Count <= 0)
			{
				return Removed.Count > 0;
			}
			return true;
		}
	}
}
