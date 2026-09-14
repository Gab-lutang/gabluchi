using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GabLuchi.Services;

public class ConnectRelayService
{
	private const string RelayWsUrl = "ws://localhost:8080/ws";
	private const string PublicIpUrl = "https://api.ipify.org";
	private const int ConnectTimeoutSeconds = 10;

	private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	private static readonly HttpClient Http = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(5)
	};

	public async Task<string> ShareLobbyAsync(string gameName, long appId, int port, CancellationToken ct = default)
	{
		string ip = await DetectPublicIpAsync(ct);
		if (string.IsNullOrEmpty(ip))
		{
			ip = DetectLocalIp();
		}
		if (string.IsNullOrEmpty(ip))
		{
			throw new InvalidOperationException("Could not detect your IP address. Check your network connection.");
		}

		var request = new
		{
			action = "host",
			gameName,
			appId,
			ip,
			port
		};

		using WebSocket ws = await ConnectAsync(ct);
		await SendJsonAsync(ws, request, ct);
		var response = await RecvJsonAsync<RelayResponse>(ws, ct);

		if (!response.Ok)
		{
			throw new InvalidOperationException(response.Error ?? "Failed to create lobby code");
		}

		return response.Code ?? throw new InvalidOperationException("No code returned from relay");
	}

	public async Task<LobbyInfo?> JoinLobbyAsync(string code, CancellationToken ct = default)
	{
		var request = new
		{
			action = "join",
			code = code.Trim().ToUpperInvariant()
		};

		using WebSocket ws = await ConnectAsync(ct);
		await SendJsonAsync(ws, request, ct);
		var response = await RecvJsonAsync<RelayResponse>(ws, ct);

		if (!response.Ok)
		{
			return null;
		}

		return response.Lobby;
	}

	private static async Task<WebSocket> ConnectAsync(CancellationToken ct)
	{
		using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		cts.CancelAfter(TimeSpan.FromSeconds(ConnectTimeoutSeconds));

		ClientWebSocket ws = new ClientWebSocket();
		await ws.ConnectAsync(new Uri(RelayWsUrl), cts.Token);
		return ws;
	}

	private static async Task SendJsonAsync<T>(WebSocket ws, T obj, CancellationToken ct)
	{
		string json = JsonSerializer.Serialize(obj, JsonOpts);
		byte[] bytes = Encoding.UTF8.GetBytes(json);
		await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
	}

	private static async Task<T> RecvJsonAsync<T>(WebSocket ws, CancellationToken ct) where T : new()
	{
		using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
		cts.CancelAfter(TimeSpan.FromSeconds(ConnectTimeoutSeconds));

		using var ms = new System.IO.MemoryStream();
		byte[] buf = new byte[4096];
		WebSocketReceiveResult result;
		do
		{
			result = await ws.ReceiveAsync(new ArraySegment<byte>(buf), cts.Token);
			if (result.MessageType == WebSocketMessageType.Close)
			{
				return new T();
			}
			ms.Write(buf, 0, result.Count);
		}
		while (!result.EndOfMessage);

		string json = Encoding.UTF8.GetString(ms.ToArray());
		return JsonSerializer.Deserialize<T>(json, JsonOpts) ?? new T();
	}

	private static async Task<string> DetectPublicIpAsync(CancellationToken ct)
	{
		try
		{
			return (await Http.GetStringAsync(PublicIpUrl, ct)).Trim();
		}
		catch
		{
			return "";
		}
	}

	private static string DetectLocalIp()
	{
		try
		{
			using Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			sock.Connect("8.8.8.8", 53);
			if (sock.LocalEndPoint is IPEndPoint ep)
			{
				return ep.Address.ToString();
			}
		}
		catch
		{
		}
		return "";
	}
}

public class RelayResponse
{
	public bool Ok { get; set; }
	public string? Code { get; set; }
	public LobbyInfo? Lobby { get; set; }
	public string? Error { get; set; }
}

public class LobbyInfo
{
	public string GameName { get; set; } = "";
	public long AppId { get; set; }
	public string Ip { get; set; } = "";
	public int Port { get; set; }
}
