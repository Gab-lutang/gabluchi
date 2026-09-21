using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GabLuchi.Services;

public class AnnouncementService : ObservableObject
{
	private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };

	private string? _announcement;
	public string? Announcement
	{
		get => _announcement;
		set => SetProperty(ref _announcement, value);
	}

	private bool _isDismissed;
	public bool IsDismissed
	{
		get => _isDismissed;
		set => SetProperty(ref _isDismissed, value);
	}

	public bool HasAnnouncement => !string.IsNullOrWhiteSpace(Announcement) && !IsDismissed;

	public async Task FetchAsync()
	{
		try
		{
			string endpoint = Config.KeyCheckerBase.TrimEnd('/') + "/api/announce";
			string json = await _http.GetStringAsync(endpoint);
			using JsonDocument doc = JsonDocument.Parse(json);
			if (doc.RootElement.TryGetProperty("announcement", out JsonElement el) && el.ValueKind == JsonValueKind.String)
			{
				Announcement = el.GetString();
			}
			else
			{
				Announcement = null;
			}
			IsDismissed = false;
			OnPropertyChanged(nameof(HasAnnouncement));
		}
		catch
		{
			Announcement = null;
			OnPropertyChanged(nameof(HasAnnouncement));
		}
	}

	public void Dismiss()
	{
		IsDismissed = true;
		OnPropertyChanged(nameof(HasAnnouncement));
	}
}
