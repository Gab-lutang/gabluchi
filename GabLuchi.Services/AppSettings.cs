namespace GabLuchi.Services;

public class AppSettings
{
	public string? SteamPathOverride { get; set; }

	public string? SelectedMode { get; set; }

	public bool? AutoUpdateApps { get; set; }

	public int? ManagePageSize { get; set; }

	public int? FixesPageSize { get; set; }

	public string? Language { get; set; }

	public string? HubcapApiKey { get; set; }

	public string? LicenseToken { get; set; }

	public string? LicenseMachineId { get; set; }

	public bool? FastFetch { get; set; }
}
