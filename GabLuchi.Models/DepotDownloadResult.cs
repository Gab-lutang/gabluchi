namespace GabLuchi.Models;

public record DepotDownloadResult(bool Success, string? Error, int FilesDownloaded, string OutputDir);
