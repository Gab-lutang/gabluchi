using System.Collections.Generic;

namespace GabLuchi.Services;

public record AppDepotInfo(long AppId, IReadOnlyList<ContentDepot> Depots, IReadOnlyList<long> DlcIds, IReadOnlyList<string> LaunchExes);
