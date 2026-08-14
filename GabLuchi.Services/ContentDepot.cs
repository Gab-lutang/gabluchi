namespace GabLuchi.Services;

public record ContentDepot(long Id, long Size, long? DlcAppId, bool IsShared, string? Os, string? Language)
{
	public bool IsDlc => DlcAppId.HasValue;
}
