namespace GabLuchi.Models;

public class GameInfo
{
	public string Name { get; set; } = "";
	public long AppId { get; set; }
	public int DefaultPort { get; set; }
	public string PortNote { get; set; } = "";
	public string Display => Name + " (" + AppId + ")";
}
