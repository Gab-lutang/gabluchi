namespace GabLuchi.Models;

public class MultiplayerFixResult
{
	public string Url { get; set; }

	public string Title { get; set; }

	public double Score { get; set; }

	public MultiplayerFixResult(string url, string title, double score)
	{
		Url = url;
		Title = title;
		Score = score;
	}
}
