namespace GabLuchi.Services;

public record SteamlessResult(int Patched, int Unchanged, int Total, string? Error)
{
	public bool Failed => Error != null;
}
