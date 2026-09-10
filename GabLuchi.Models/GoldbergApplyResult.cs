namespace GabLuchi.Models;

public record GoldbergApplyResult(int Replaced, int Skipped, string? Error)
{
	public bool Failed => Error != null;
}
