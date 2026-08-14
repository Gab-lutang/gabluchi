namespace GabLuchi.Services;

public sealed record PluginStatus(bool FrontendInstalled, bool DllInstalled, bool DllMatches, string? InstalledTag, string? LatestTag, bool UpdateAvailable, bool MillenniumPresent, bool Offline, bool Port8080Busy);
