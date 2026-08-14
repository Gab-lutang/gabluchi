namespace GabLuchi.Services;

internal record ApiSource(string Name, string Url, int SuccessCode, bool RequiresAuth = false);
