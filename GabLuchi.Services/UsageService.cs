using System;
using System.ComponentModel;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GabLuchi.Services;

public record UsageCheckResult(bool Allowed, string Tier, bool Expired, int DownloadsUsed, int DownloadsLimit, int MultiplayerUsed, int MultiplayerLimit);

public class UsageService : INotifyPropertyChanged
{
	private readonly LicenseService _license;
	private readonly AuthService _auth;
	private readonly SettingsService _settings;
	private readonly ToastService _toast;

	private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

	private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

	private string KeyCheckerBase => Config.KeyCheckerBase;

	public event PropertyChangedEventHandler? PropertyChanged;

	private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

	public UsageService(LicenseService license, AuthService auth, SettingsService settings, ToastService toast)
	{
		_license = license;
		_auth = auth;
		_settings = settings;
		_toast = toast;
	}

	private string _currentTier = "paid";
	public string CurrentTier
	{
		get => _currentTier;
		private set { _currentTier = value; OnPropertyChanged(nameof(CurrentTier)); OnPropertyChanged(nameof(IsFreeTier)); OnPropertyChanged(nameof(UsageText)); }
	}

	private DateTime? _expiresAt;
	public DateTime? ExpiresAt
	{
		get => _expiresAt;
		private set { _expiresAt = value; OnPropertyChanged(nameof(ExpiresAt)); OnPropertyChanged(nameof(IsExpired)); OnPropertyChanged(nameof(UsageText)); }
	}

	public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
	public bool IsFreeTier => CurrentTier == "free";

	private int _downloadsUsed;
	public int DownloadsUsed
	{
		get => _downloadsUsed;
		private set { _downloadsUsed = value; OnPropertyChanged(nameof(DownloadsUsed)); OnPropertyChanged(nameof(UsageText)); }
	}

	private int _downloadsLimit;
	public int DownloadsLimit
	{
		get => _downloadsLimit;
		private set { _downloadsLimit = value; OnPropertyChanged(nameof(DownloadsLimit)); OnPropertyChanged(nameof(UsageText)); }
	}

	private int _multiplayerUsed;
	public int MultiplayerUsed
	{
		get => _multiplayerUsed;
		private set { _multiplayerUsed = value; OnPropertyChanged(nameof(MultiplayerUsed)); OnPropertyChanged(nameof(UsageText)); }
	}

	private int _multiplayerLimit;
	public int MultiplayerLimit
	{
		get => _multiplayerLimit;
		private set { _multiplayerLimit = value; OnPropertyChanged(nameof(MultiplayerLimit)); OnPropertyChanged(nameof(UsageText)); }
	}

	public string UsageText
	{
		get
		{
			if (CurrentTier == "paid") return "Unlimited";
			if (IsExpired) return "Expired — run /freekey on Discord";
			return $"Downloads: {DownloadsUsed}/{DownloadsLimit} | Multi: {MultiplayerUsed}/{MultiplayerLimit} this week";
		}
	}

	public void SetTier(string tier, string? expiresAt)
	{
		CurrentTier = tier ?? "paid";
		if (DateTime.TryParse(expiresAt, out var exp))
			ExpiresAt = exp;
		else
			ExpiresAt = null;
	}

	public async Task<UsageCheckResult?> CheckUsageAsync(string action, long? appId = null, CancellationToken ct = default)
	{
		if (!_license.IsActivated) return null;
		if (CurrentTier == "paid")
		{
			return new UsageCheckResult(true, "paid", false, 0, 0, 0, 0);
		}
		try
		{
			string machineId = LicenseService.ComputeMachineId();
			string discordUserId = _auth.UserId ?? "";
			var payload = new
			{
				machineId,
				discordUserId,
				action,
				appId = appId ?? (object)null!
			};
			string json = JsonSerializer.Serialize(payload);
			using var content = new StringContent(json, Encoding.UTF8, "application/json");
			using HttpResponseMessage res = await _http.PostAsync(KeyCheckerBase.TrimEnd('/') + "/api/usage/track", content, ct);
			string body = await res.Content.ReadAsStringAsync(ct);
			using JsonDocument doc = JsonDocument.Parse(body);

			bool allowed = doc.RootElement.TryGetProperty("allowed", out var aEl) && aEl.GetBoolean();
			string tier = doc.RootElement.TryGetProperty("tier", out var tEl) ? tEl.GetString() ?? "free" : "free";
			bool expired = doc.RootElement.TryGetProperty("expired", out var eEl) && eEl.GetBoolean();
			int dlUsed = doc.RootElement.TryGetProperty("downloadsUsed", out var dlEl) && dlEl.TryGetInt32(out var dv) ? dv : 0;
			int dlLimit = doc.RootElement.TryGetProperty("downloadsLimit", out var dllEl) && dllEl.TryGetInt32(out var dlv) ? dlv : 2;
			int mpUsed = doc.RootElement.TryGetProperty("multiplayerUsed", out var mpEl) && mpEl.TryGetInt32(out var mv) ? mv : 0;
			int mpLimit = doc.RootElement.TryGetProperty("multiplayerLimit", out var mplEl) && mplEl.TryGetInt32(out var mplv) ? mplv : 2;

			DownloadsUsed = dlUsed;
			DownloadsLimit = dlLimit;
			MultiplayerUsed = mpUsed;
			MultiplayerLimit = mpLimit;

			return new UsageCheckResult(allowed, tier, expired, dlUsed, dlLimit, mpUsed, mpLimit);
		}
		catch
		{
			return new UsageCheckResult(false, "free", false, 0, 0, 0, 0);
		}
	}

	public void ShowLimitToast(string action)
	{
		if (action == "download")
			_toast.Show("Usage Limit", "Free tier: 2 downloads per week. Upgrade or wait for reset.", error: true);
		else
			_toast.Show("Usage Limit", "Free tier: 2 multiplayer sessions per week. Upgrade or wait for reset.", error: true);
	}
}
