namespace GabLuchi.Models;

public sealed record ModeDefinition(UnlockerMode Mode, string DisplayName, string Description, ModeKind Kind, string Owner, string Repo, string? FixedTag, string[] PlaceFiles, string? ZipAssetPattern, string? CliAssetName, string? CliArgs, string? VerifyFile, string? HiddenUnlessFile = null);
