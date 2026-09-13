namespace GabLuchi.Models;

public class OnlineFixApplyResult
{
	public bool Success { get; set; }

	public int FilesInstalled { get; set; }

	public string? Error { get; set; }

	public string? GameDir { get; set; }

	public OnlineFixApplyResult(bool success, int filesInstalled, string? error, string? gameDir)
	{
		Success = success;
		FilesInstalled = filesInstalled;
		Error = error;
		GameDir = gameDir;
	}
}
