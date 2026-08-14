using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GabLuchi;
using GabLuchi.Models;

namespace GabLuchi.Services;

public class LicenseService
{
	private readonly SettingsService _settings;

	private readonly HttpClient _http = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(20.0)
	};

	private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true
	};

	public LicenseService(SettingsService settings)
	{
		_settings = settings;
	}

	public string? Token => Decrypt(_settings.LicenseToken);

	public string? BoundMachineId => _settings.LicenseMachineId;

	public bool IsActivated => !string.IsNullOrWhiteSpace(Token) && !string.IsNullOrWhiteSpace(BoundMachineId);

	private string KeyCheckerBase => Config.KeyCheckerBase;

	public static bool IsValidKeyFormat(string? key)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			return false;
		}
		string text = key.Trim();
		if (text.Length != 19)
		{
			return false;
		}
		for (int i = 0; i < 19; i++)
		{
			if (i == 4 || i == 9 || i == 14)
			{
				if (text[i] != '-')
				{
					return false;
				}
			}
			else if (!char.IsLetterOrDigit(text[i]))
			{
				return false;
			}
		}
		return true;
	}

	public static string ComputeMachineId()
	{
		string volumeSerial = GetVolumeSerial();
		string raw = string.Join("|", volumeSerial, Environment.MachineName, Environment.UserName);
		byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
		StringBuilder stringBuilder = new StringBuilder(hash.Length * 2);
		foreach (byte b in hash)
		{
			stringBuilder.Append(b.ToString("x2"));
		}
		return stringBuilder.ToString();
	}

	public async Task<LicenseActivateResult> ActivateAsync(string key, string discordUserId)
	{
		string machineId = ComputeMachineId();
		string url = KeyCheckerBase.TrimEnd('/') + "/activate";
		using StringContent content = new StringContent(JsonSerializer.Serialize(new { key = key.Trim(), machineId, discordUserId = discordUserId.Trim() }), Encoding.UTF8, "application/json");
		try
		{
			using HttpResponseMessage res = await _http.PostAsync(url, content);
			string body = await res.Content.ReadAsStringAsync();
			string? error = null;
			try
			{
				using JsonDocument doc = JsonDocument.Parse(body);
				if (doc.RootElement.TryGetProperty("error", out JsonElement e))
				{
					error = e.GetString();
				}
			}
			catch
			{
			}
			if (!res.IsSuccessStatusCode)
			{
				return LicenseActivateResult.Failure(string.IsNullOrWhiteSpace(error) ? "server-error" : error!);
			}
			string? token = null;
			try
			{
				using JsonDocument doc = JsonDocument.Parse(body);
				if (doc.RootElement.TryGetProperty("token", out JsonElement t))
				{
					token = t.GetString();
				}
			}
			catch
			{
			}
			if (string.IsNullOrWhiteSpace(token))
			{
				return LicenseActivateResult.Failure("server-error");
			}
			_settings.LicenseMachineId = machineId;
			_settings.LicenseToken = Encrypt(token);
			return LicenseActivateResult.Success(token);
		}
		catch (Exception)
		{
			return LicenseActivateResult.Failure("unreachable");
		}
	}

	public void Deactivate()
	{
		_settings.LicenseToken = null;
		_settings.LicenseMachineId = null;
	}

	public string? GetDownloadUrl(string appid)
	{
		if (!IsActivated)
		{
			return null;
		}
		return KeyCheckerBase.TrimEnd('/') + "/manifest/" + Uri.EscapeDataString(appid) + "?token=" + Uri.EscapeDataString(Token!);
	}

	public async Task<LicenseAccount?> GetAccountAsync(CancellationToken ct = default(CancellationToken))
	{
		if (string.IsNullOrWhiteSpace(Token))
		{
			return null;
		}
		try
		{
			string url = KeyCheckerBase.TrimEnd('/') + "/account?token=" + Uri.EscapeDataString(Token!);
			using HttpResponseMessage res = await _http.GetAsync(url, ct);
			string body = await res.Content.ReadAsStringAsync(ct);
			return JsonSerializer.Deserialize<LicenseAccount>(body, JsonOpts);
		}
		catch
		{
			return null;
		}
	}

	private static string? Encrypt(string? value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return null;
		}
		byte[] data = ProtectedData.Protect(Encoding.UTF8.GetBytes(value), null, DataProtectionScope.CurrentUser);
		return Convert.ToBase64String(data);
	}

	private static string? Decrypt(string? value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return null;
		}
		try
		{
			byte[] data = ProtectedData.Unprotect(Convert.FromBase64String(value), null, DataProtectionScope.CurrentUser);
			return Encoding.UTF8.GetString(data);
		}
		catch
		{
			return null;
		}
	}

	[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
	private static extern bool GetVolumeInformation(string rootPathName, StringBuilder volumeNameBuffer, int volumeNameSize, out uint volumeSerialNumber, out uint maximumComponentLength, out uint fileSystemFlags, StringBuilder fileSystemNameBuffer, int fileSystemNameSize);

	private static string GetVolumeSerial()
	{
		try
		{
			StringBuilder volumeName = new StringBuilder(261);
			StringBuilder fileSystemName = new StringBuilder(261);
			if (GetVolumeInformation("C:\\", volumeName, 260, out uint serial, out uint _, out uint _, fileSystemName, 260))
			{
				return serial.ToString("X8");
			}
		}
		catch
		{
		}
		return "unknown";
	}
}
