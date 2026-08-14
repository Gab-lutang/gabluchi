using System.Collections.Generic;
using System.Threading;

namespace GabLuchi.Services;

public record DownloadState
{
	public string Status { get; set; } = "queued";

	public long BytesRead { get; set; }

	public long TotalBytes { get; set; }

	public string? CurrentApi { get; set; }

	public Dictionary<string, object> ApiErrors { get; set; } = new Dictionary<string, object>();

	public string? Error { get; set; }

	public string? InstalledPath { get; set; }

	public bool Success { get; set; }

	public string? Api { get; set; }

	public CancellationTokenSource? Cts { get; set; }
}
