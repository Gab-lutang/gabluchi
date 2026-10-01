using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GabLuchi.Services;

/// <summary>
/// Resolves host names through Cloudflare's DNS-over-HTTPS endpoint, for users whose ISP DNS
/// blocks or hijacks the hosts we depend on.
/// </summary>
/// <remarks>
/// <para>
/// The endpoint is addressed by IP LITERAL (<c>https://1.1.1.1/dns-query</c>), never by name.
/// Resolving "one.one.one.one" would need working DNS, which is exactly what is missing.
/// Cloudflare's certificate carries 1.1.1.1 in its SANs, so TLS still validates against the
/// literal normally.
/// </para>
/// <para>
/// Every failure yields an empty array rather than throwing. A resolver that cannot answer must
/// let the caller fall back to the system resolver, not take the request down.
/// </para>
/// </remarks>
public class DohResolver
{
	/// <summary>IP literal on purpose. See the class remarks.</summary>
	public const string Endpoint = "https://1.1.1.1/dns-query";

	// A short floor stops a hostile-but-valid TTL of 0 turning every connection into a DoH
	// round-trip; the cap keeps a very long TTL from pinning us to an address that has moved.
	private static readonly TimeSpan MinTtl = TimeSpan.FromSeconds(30);
	private static readonly TimeSpan MaxTtl = TimeSpan.FromMinutes(5);

	private readonly HttpClient _http;
	private readonly ConcurrentDictionary<string, (IPAddress[] Addresses, DateTimeOffset Expires)> _cache = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>Injectable clock so cache expiry is testable without sleeping.</summary>
	internal Func<DateTimeOffset> UtcNow { get; init; } = () => DateTimeOffset.UtcNow;

	public DohResolver(HttpMessageHandler? handler = null)
	{
		// Its OWN HttpClient on a stock handler, deliberately not the shared one in
		// DohHttpMessageHandler. Routing the resolver through the handler that calls the resolver
		// would recurse on the very first lookup.
		_http = handler is null ? new HttpClient { Timeout = TimeSpan.FromSeconds(5) } : new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
		_http.DefaultRequestHeaders.Accept.Add(new("application/dns-json"));
	}

	/// <summary>
	/// Names that must never go to DoH: it either cannot answer them or does not need to.
	/// </summary>
	/// <remarks>
	/// IP literals need no resolution at all (the manifest backend is one). Loopback and
	/// single-label names are the local HTTP server, the plugin port probe and the OAuth callback,
	/// which a public resolver has no answer for.
	/// </remarks>
	public static bool ShouldBypass(string? host)
	{
		if (string.IsNullOrWhiteSpace(host))
		{
			return true;
		}
		if (IPAddress.TryParse(host, out _))
		{
			return true;
		}
		if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return !host.Contains('.');
	}

	/// <summary>
	/// Addresses for <paramref name="host"/>, or an EMPTY array if DoH could not answer. Never
	/// throws (except on caller cancellation).
	/// </summary>
	public async Task<IPAddress[]> ResolveAsync(string host, CancellationToken ct = default)
	{
		if (ShouldBypass(host))
		{
			return [];
		}
		if (_cache.TryGetValue(host, out (IPAddress[] Addresses, DateTimeOffset Expires) hit) && hit.Expires > UtcNow())
		{
			return hit.Addresses;
		}
		try
		{
			string url = $"{Endpoint}?name={Uri.EscapeDataString(host)}&type=A";
			using HttpResponseMessage resp = await _http.GetAsync(url, ct);
			if (!resp.IsSuccessStatusCode)
			{
				return [];
			}
			string body = await resp.Content.ReadAsStringAsync(ct);
			(IPAddress[] addresses, TimeSpan ttl) = Parse(body);
			if (addresses.Length == 0)
			{
				return [];
			}
			TimeSpan lifetime = ttl < MinTtl ? MinTtl : ttl > MaxTtl ? MaxTtl : ttl;
			_cache[host] = (addresses, UtcNow() + lifetime);
			return addresses;
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			throw;
		}
		catch
		{
			return [];
		}
	}

	/// <summary>
	/// Pulls the A/AAAA records out of a Cloudflare dns-json body. Returns empty on anything
	/// unexpected, so the caller falls back instead of failing the request.
	/// </summary>
	/// <remarks>
	/// A non-zero <c>Status</c> is a real DNS error (NXDOMAIN is 3) and must yield nothing: the
	/// caller has to fall back rather than treat "no such host" as an answer.
	/// </remarks>
	public static (IPAddress[] Addresses, TimeSpan Ttl) Parse(string json)
	{
		try
		{
			using JsonDocument doc = JsonDocument.Parse(json);
			JsonElement root = doc.RootElement;
			if (root.ValueKind != JsonValueKind.Object)
			{
				return ([], default);
			}
			if (!root.TryGetProperty("Status", out JsonElement status) || status.GetInt32() != 0)
			{
				return ([], default);
			}
			if (!root.TryGetProperty("Answer", out JsonElement answer) || answer.ValueKind != JsonValueKind.Array)
			{
				return ([], default);
			}
			System.Collections.Generic.List<IPAddress> addresses = new System.Collections.Generic.List<IPAddress>();
			int ttl = int.MaxValue;
			foreach (JsonElement entry in answer.EnumerateArray())
			{
				// type 1 = A, 28 = AAAA. Anything else in the chain (5 = CNAME) is a step, not an answer.
				if (!entry.TryGetProperty("type", out JsonElement type))
				{
					continue;
				}
				int t = type.GetInt32();
				if (t != 1 && t != 28)
				{
					continue;
				}
				if (!entry.TryGetProperty("data", out JsonElement data))
				{
					continue;
				}
				if (!IPAddress.TryParse(data.GetString(), out IPAddress? ip))
				{
					continue;
				}
				addresses.Add(ip);
				if (entry.TryGetProperty("TTL", out JsonElement recordTtl) && recordTtl.TryGetInt32(out int v) && v < ttl)
				{
					ttl = v;
				}
			}
			if (addresses.Count == 0)
			{
				return ([], default);
			}
			return ([.. addresses], TimeSpan.FromSeconds(ttl == int.MaxValue ? 0 : Math.Max(0, ttl)));
		}
		catch
		{
			return ([], default);
		}
	}
}

/// <summary>
/// A <see cref="SocketsHttpHandler"/> whose connect step prefers DoH-resolved addresses and falls
/// back to system DNS. Attach it to any HttpClient that talks to a hostname.
/// </summary>
/// <remarks>
/// Falls back rather than fails: if DoH is unreachable the connection is attempted exactly as it
/// would have been before this handler existed.
/// </remarks>
public static class DohHttp
{
	private static DohResolver? _shared;

	private static DohResolver Shared => _shared ??= new DohResolver();

	/// <summary>
	/// Builds a handler with DoH-backed connect resolution and sensible pooling defaults.
	/// </summary>
	public static SocketsHttpHandler CreateHandler(bool allowAutoRedirect = true)
	{
		SocketsHttpHandler handler = new SocketsHttpHandler
		{
			AllowAutoRedirect = allowAutoRedirect,
			AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
			ConnectTimeout = TimeSpan.FromSeconds(15),
			PooledConnectionLifetime = TimeSpan.FromMinutes(5)
		};
		handler.ConnectCallback = ConnectAsync;
		return handler;
	}

	/// <summary>Convenience factory for a client already wired to DoH.</summary>
	public static HttpClient CreateClient(TimeSpan? timeout = null)
	{
		return new HttpClient(CreateHandler()) { Timeout = timeout ?? TimeSpan.FromMinutes(10) };
	}

	private static async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext ctx, CancellationToken ct)
	{
		string host = ctx.DnsEndPoint.Host;
		int port = ctx.DnsEndPoint.Port;

		IPAddress[] addresses = [];
		if (IPAddress.TryParse(host, out IPAddress? literal))
		{
			// 167.235.229.108 and friends: no resolution required.
			addresses = [literal];
		}
		else
		{
			IPAddress[] viaDoh = await Shared.ResolveAsync(host, ct);
			if (viaDoh.Length > 0)
			{
				addresses = viaDoh;
			}
			else
			{
				try
				{
					addresses = await Dns.GetHostAddressesAsync(host, ct);
				}
				catch
				{
					addresses = [];
				}
			}
		}

		Exception? last = null;
		foreach (IPAddress address in addresses)
		{
			try
			{
				Socket socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
				try
				{
					await socket.ConnectAsync(new IPEndPoint(address, port), ct);
					return new NetworkStream(socket, ownsSocket: true);
				}
				catch
				{
					socket.Dispose();
					throw;
				}
			}
			catch (OperationCanceledException) when (ct.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception ex)
			{
				last = ex;
			}
		}

		throw last ?? new SocketException((int)SocketError.HostNotFound);
	}
}
