namespace GabLuchi.Models;

public class OnlineFixEntry
{
	public long AppId { get; set; }

	public string GameName { get; set; } = "";

	public string FileName { get; set; } = "";

	public long SizeBytes { get; set; }

	public string DownloadUrl { get; set; } = "";

	public OnlineFixEntry(long appId, string gameName, string fileName, long sizeBytes, string downloadUrl)
	{
		AppId = appId;
		GameName = gameName;
		FileName = fileName;
		SizeBytes = sizeBytes;
		DownloadUrl = downloadUrl;
	}
}
