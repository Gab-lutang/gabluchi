using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GabLuchi;

namespace GabLuchi.Services;

public class AuthService
{
	private static readonly string AuthFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GabLuchi", "auth.dat");

	private static readonly string RedirectUri = "http://localhost:53789/callback";

	private static readonly string RedirectPrefix = "http://localhost:53789/";

	private readonly HttpClient _http = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(30.0)
	};

	private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true
	};

	private string? _token;

	private DateTimeOffset _expiresAt;

	public string? DisplayName { get; private set; }

	public string? UserId { get; private set; }

	public string? AvatarUrl { get; private set; }

	public bool IsSignedIn => _token != null;

	public bool IsGuest => !IsSignedIn;

	public event Action? AuthStateChanged;

	public async Task<bool> InitializeAsync()
	{
		StoredAuth? stored = LoadStored();
		if (stored == null || string.IsNullOrEmpty(stored.Token))
		{
			Log("restore: no stored token -> clear");
			ClearSession();
			AuthStateChanged?.Invoke();
			return false;
		}
		_token = stored.Token;
		_expiresAt = stored.ExpiresAt;
		DisplayName = stored.DisplayName;
		UserId = stored.UserId;
		AvatarUrl = stored.AvatarUrl;
		if (_expiresAt <= DateTimeOffset.UtcNow)
		{
			Log("restore: token expired " + _expiresAt.ToString("O") + " -> clear");
			ClearSession();
			AuthStateChanged?.Invoke();
			return false;
		}
		Log("restore: token present, user=" + (UserId ?? "null") + ", validating...");
		bool valid = await ValidateAsync();
		Log("restore: validate=" + valid);
		AuthStateChanged?.Invoke();
		return valid;
	}

	public async Task SignInAsync(CancellationToken ct = default(CancellationToken))
	{
		string verifier = CreateCodeVerifier();
		string challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
		string authorizeUrl = "https://discord.com/oauth2/authorize?"
			+ "client_id=" + Config.DiscordClientId
			+ "&response_type=code"
			+ "&redirect_uri=" + Uri.EscapeDataString(RedirectUri)
			+ "&scope=" + Uri.EscapeDataString("identify guilds")
			+ "&code_challenge=" + challenge
			+ "&code_challenge_method=S256"
			+ "&prompt=consent";
		using HttpListener listener = new HttpListener();
		listener.Prefixes.Add(RedirectPrefix);
		listener.Start();
		Log("signin: listener started on " + RedirectPrefix);
		Log("signin: auth backend=" + Config.AuthBackendBase + ", client_id=" + Config.DiscordClientId + ", redirect_uri=" + RedirectUri);
		Process.Start(new ProcessStartInfo(authorizeUrl)
		{
			UseShellExecute = true
		});
		Log("signin: browser opened, waiting for callback...");
		string code;
		try
		{
			code = await WaitForCallbackAsync(listener, ct);
		}
		finally
		{
			listener.Stop();
		}
		Log("signin: callback received, code length=" + (code?.Length ?? 0));
		try
		{
			ApplySession(await ExchangeCodeAsync(code, verifier, ct));
		}
		catch (Exception ex)
		{
			Log("signin: FAILED - " + ex.Message);
			throw;
		}
		Log("signin: session applied, user=" + (UserId ?? "null"));
		AuthStateChanged?.Invoke();
	}

	private static async Task<string> WaitForCallbackAsync(HttpListener listener, CancellationToken ct)
	{
		using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
		timeout.CancelAfter(TimeSpan.FromMinutes(5.0));
		HttpListenerContext ctx;
		string code;
		string error;
		while (true)
		{
			try
			{
				ctx = await listener.GetContextAsync().WaitAsync(timeout.Token);
			}
			catch (OperationCanceledException)
			{
				throw new ApiException("Sign-in timed out — no response from the browser.");
			}
			code = Query(ctx.Request.Url?.Query).Get("code");
			error = Query(ctx.Request.Url?.Query).Get("error_description");
			if (code != null || error != null)
			{
				break;
			}
			ctx.Response.StatusCode = 404;
			ctx.Response.Close();
		}
		bool ok = code != null;
		byte[] bytes = Encoding.UTF8.GetBytes(ResultPage(ok, error));
		ctx.Response.ContentType = "text/html; charset=utf-8";
		ctx.Response.ContentLength64 = bytes.Length;
		await ctx.Response.OutputStream.WriteAsync(bytes, timeout.Token);
		ctx.Response.Close();
		if (!ok)
		{
			throw new ApiException(error ?? "Sign-in was denied.");
		}
		return code;
	}

	private async Task<ExchangeResponse> ExchangeCodeAsync(string code, string verifier, CancellationToken ct)
	{
		HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, Config.AuthBackendBase + "/api/auth/exchange")
		{
			Content = new StringContent(JsonSerializer.Serialize(new
			{
				code,
				code_verifier = verifier
			}), Encoding.UTF8, "application/json")
		};
		HttpResponseMessage res = await _http.SendAsync(request, ct);
		string text = await res.Content.ReadAsStringAsync(ct);
		if (!res.IsSuccessStatusCode)
		{
			string message = "Sign-in failed";
			try
			{
				message = JsonSerializer.Deserialize<JsonElement>(text).GetProperty("error").GetString() ?? message;
			}
			catch
			{
			}
			throw new ApiException(message + $" ({(int)res.StatusCode})");
		}
		return JsonSerializer.Deserialize<ExchangeResponse>(text, JsonOpts) ?? throw new ApiException("Token exchange returned an empty session.");
	}

	private async Task<bool> ValidateAsync()
	{
		try
		{
			HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, Config.AuthBackendBase + "/api/auth/me");
			request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
			HttpResponseMessage res = await _http.SendAsync(request);
			if (!res.IsSuccessStatusCode)
			{
				ClearSession();
				return false;
			}
			string text = await res.Content.ReadAsStringAsync();
			MeResponse? me = JsonSerializer.Deserialize<MeResponse>(text, JsonOpts);
			if (me != null)
			{
				DisplayName = me.DisplayName;
				UserId = me.UserId;
				AvatarUrl = me.AvatarUrl;
				SaveStored(new StoredAuth(_token, _expiresAt, DisplayName, UserId, AvatarUrl));
			}
			return true;
		}
		catch
		{
			return _expiresAt > DateTimeOffset.UtcNow;
		}
	}

	public async Task<string> GetValidAccessTokenAsync()
	{
		if (_token == null)
		{
			throw new ApiException("Sign in to download from this source.");
		}
		if (_expiresAt <= DateTimeOffset.UtcNow)
		{
			ClearSession();
			AuthStateChanged?.Invoke();
			throw new ApiException("Your session expired — sign in again.");
		}
		await Task.CompletedTask;
		return _token;
	}

	public void SignOut()
	{
		Log("signout");
		ClearSession();
		AuthStateChanged?.Invoke();
	}

	private void ApplySession(ExchangeResponse session)
	{
		if (string.IsNullOrWhiteSpace(session.Token))
		{
			throw new ApiException("Sign-in returned an empty session — please try again.");
		}
		_token = session.Token;
		_expiresAt = DateTimeOffset.UtcNow.AddSeconds(session.ExpiresIn);
		DisplayName = session.DisplayName;
		UserId = session.UserId;
		AvatarUrl = session.AvatarUrl;
		SaveStored(new StoredAuth(_token, _expiresAt, DisplayName, UserId, AvatarUrl));
	}

	private void ClearSession()
	{
		_token = null;
		_expiresAt = default(DateTimeOffset);
		AvatarUrl = null;
		UserId = null;
		DisplayName = null;
		try
		{
			File.Delete(AuthFile);
		}
		catch
		{
		}
	}

	private static StoredAuth? LoadStored()
	{
		try
		{
			if (!File.Exists(AuthFile))
			{
				return null;
			}
			return JsonSerializer.Deserialize<StoredAuth>(ProtectedData.Unprotect(File.ReadAllBytes(AuthFile), null, DataProtectionScope.CurrentUser));
		}
		catch
		{
			return null;
		}
	}

	private static void SaveStored(StoredAuth auth)
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(AuthFile) ?? ".");
			byte[] bytes = ProtectedData.Protect(JsonSerializer.SerializeToUtf8Bytes(auth), null, DataProtectionScope.CurrentUser);
			int num = 0;
			while (true)
			{
				try
				{
					File.WriteAllBytes(AuthFile, bytes);
					break;
				}
				catch (IOException) when (num < 3)
				{
					Thread.Sleep(150);
				}
				catch
				{
					break;
				}
				num++;
			}
		}
		catch
		{
		}
	}

	private static void Log(string message)
	{
		try
		{
			File.AppendAllText(Path.Combine(Path.GetTempPath(), "gabluchi_auth.log"), DateTimeOffset.Now.ToString("HH:mm:ss.fff") + " " + message + Environment.NewLine);
		}
		catch
		{
		}
	}

	private static string CreateCodeVerifier()
	{
		return Base64Url(RandomNumberGenerator.GetBytes(48));
	}

	private static string Base64Url(byte[] bytes)
	{
		return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
	}

	private static System.Collections.Specialized.NameValueCollection Query(string? query)
	{
		System.Collections.Specialized.NameValueCollection result = new System.Collections.Specialized.NameValueCollection();
		if (string.IsNullOrEmpty(query))
		{
			return result;
		}
		string trimmed = query[0] == '?' ? query.Substring(1) : query;
		foreach (string pair in trimmed.Split('&'))
		{
			if (string.IsNullOrEmpty(pair))
			{
				continue;
			}
			int idx = pair.IndexOf('=');
			if (idx < 0)
			{
				result[Uri.UnescapeDataString(pair)] = "";
			}
			else
			{
				result[Uri.UnescapeDataString(pair.Substring(0, idx))] = Uri.UnescapeDataString(pair.Substring(idx + 1));
			}
		}
		return result;
	}

	private static string ResultPage(bool ok, string? error)
	{
		return $"<!doctype html>\n<html><head><meta charset=\"utf-8\"><title>GabLuchi</title>\n<style>\n  body {{ background:#0b0b12; color:#e5e7eb; font-family:'Segoe UI',sans-serif;\n         display:flex; align-items:center; justify-content:center; height:100vh; margin:0; }}\n  .card {{ text-align:center; padding:2.5rem 3rem; background:#14141c;\n          border:1px solid rgba(255,255,255,.08); border-radius:14px; }}\n  h1 {{ font-size:1.3rem; margin:0 0 .5rem; color:{(ok ? "#a78bfa" : "#f87171")}; }}\n  p {{ color:#9ca3af; font-size:.95rem; margin:0; }}\n</style></head>\n<body><div class=\"card\">\n  <h1>{(ok ? "Signed in!" : "Sign-in failed")}</h1>\n  <p>{(ok ? "You can close this tab and return to GabLuchi." : WebUtility.HtmlEncode(error ?? "Please try again from the app."))}</p>\n</div></body></html>";
	}

	private sealed class StoredAuth
	{
		public string? Token { get; set; }

		public DateTimeOffset ExpiresAt { get; set; }

		public string? DisplayName { get; set; }

		public string? UserId { get; set; }

		public string? AvatarUrl { get; set; }

		public StoredAuth()
		{
		}

		public StoredAuth(string? token, DateTimeOffset expiresAt, string? displayName, string? userId, string? avatarUrl)
		{
			Token = token;
			ExpiresAt = expiresAt;
			DisplayName = displayName;
			UserId = userId;
			AvatarUrl = avatarUrl;
		}
	}

	private sealed class ExchangeResponse
	{
		public string Token { get; set; } = "";

		public int ExpiresIn { get; set; }

		public string? DisplayName { get; set; }

		public string? UserId { get; set; }

		public string? AvatarUrl { get; set; }
	}

	private sealed class MeResponse
	{
		public string? DisplayName { get; set; }

		public string? UserId { get; set; }

		public string? AvatarUrl { get; set; }
	}
}
