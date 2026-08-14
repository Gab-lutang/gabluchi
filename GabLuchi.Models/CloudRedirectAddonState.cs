namespace GabLuchi.Models;

public sealed record CloudRedirectAddonState(bool Installed, bool Enabled, bool UpdateAvailable, string? LatestVersion);
