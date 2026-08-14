namespace GabLuchi.Models;

public class LicenseActivateResult
{
	public bool Ok { get; }

	public string? Error { get; }

	public string? Token { get; }

	private LicenseActivateResult(bool ok, string? error, string? token)
	{
		Ok = ok;
		Error = error;
		Token = token;
	}

	public static LicenseActivateResult Success(string token)
	{
		return new LicenseActivateResult(true, null, token);
	}

	public static LicenseActivateResult Failure(string error)
	{
		return new LicenseActivateResult(false, error, null);
	}
}
