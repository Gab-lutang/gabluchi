namespace GabLuchi.Models;

public class LicenseActivateResult
{
	public bool Ok { get; }

	public string? Error { get; }

	public string? Token { get; }

	public string? Tier { get; }

	public string? ExpiresAt { get; }

	private LicenseActivateResult(bool ok, string? error, string? token, string? tier, string? expiresAt)
	{
		Ok = ok;
		Error = error;
		Token = token;
		Tier = tier;
		ExpiresAt = expiresAt;
	}

	public static LicenseActivateResult Success(string token, string? tier = null, string? expiresAt = null)
	{
		return new LicenseActivateResult(true, null, token, tier, expiresAt);
	}

	public static LicenseActivateResult Failure(string error)
	{
		return new LicenseActivateResult(false, error, null, null, null);
	}
}
