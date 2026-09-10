namespace GabLuchi.Services;

public record SteamlessResult(int Patched, int Unchanged, int Total, string? Error, int GoldbergReplaced = 0, bool BypassApplied = false)
{
	public bool Failed => Error != null;
}
