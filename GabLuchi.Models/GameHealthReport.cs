using System;
using System.Collections.Generic;
using System.Linq;

namespace GabLuchi.Models;

public record GameHealthReport(
	long AppId,
	string GameName,
	string InstallDir,
	int HealthScore,
	IReadOnlyList<HealthIssue> Issues,
	DateTime ScannedAt
)
{
	public bool IsHealthy => HealthScore >= 90;
	public int CriticalCount => Issues.Count(i => i.Severity == HealthSeverity.Critical);
	public int WarningCount => Issues.Count(i => i.Severity == HealthSeverity.Warning);
	public string ScoreColor => HealthScore >= 90 ? "#22c55e" : HealthScore >= 60 ? "#eab308" : "#ef4444";
	public string ScoreLabel => HealthScore >= 90 ? "Healthy" : HealthScore >= 60 ? "Needs Attention" : "Critical";
}
