using System;
using System.Threading.Tasks;

namespace GabLuchi.Models;

public record HealthIssue(
	HealthSeverity Severity,
	string Category,
	string Title,
	string Description,
	Func<Task<bool>>? FixAction = null
)
{
	public bool HasFix => FixAction != null;
}
