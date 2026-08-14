namespace GabLuchi.Models;

public sealed record ModeState(UnlockerMode Mode, ModeStatus Status, bool IsActive, string? LatestVersion);
