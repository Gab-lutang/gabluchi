namespace GabLuchi.Models;

public record RestoreResult(int Restored, int Deleted, string? Error)
{
	public bool Failed => Error != null;
}
