using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace GabLuchi.Services;

public class CoverCache
{
	private static readonly string CoversDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GabLuchi", "covers");

	private const int MinValidBytes = 512;

	private const int PlaceholderLength = 9816;

	private const string PlaceholderSha256 = "732ec27f2af650fe079f1c83b0bb0c712a322dc175f383504176724675ad2700";

	private readonly HttpClient _http = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(20.0)
	};

	private readonly SemaphoreSlim _ioGate = new SemaphoreSlim(1, 1);

	private readonly ConcurrentDictionary<long, Task<string?>> _inFlight = new ConcurrentDictionary<long, Task<string>>();

	private readonly ConcurrentDictionary<long, byte> _noCover = new ConcurrentDictionary<long, byte>();

	private static bool IsHeaderCapsulePlaceholder(byte[] b)
	{
		if (b.Length != 9816)
		{
			return false;
		}
		return Convert.ToHexString(SHA256.HashData(b)).Equals("732ec27f2af650fe079f1c83b0bb0c712a322dc175f383504176724675ad2700", StringComparison.OrdinalIgnoreCase);
	}

	private static string PathFor(long appid)
	{
		return Path.Combine(CoversDir, $"{appid}.jpg");
	}

	private static bool IsJpeg(byte[] b)
	{
		if (b.Length >= 3 && b[0] == byte.MaxValue && b[1] == 216)
		{
			return b[2] == byte.MaxValue;
		}
		return false;
	}

	public string? GetLocalPath(long appid)
	{
		string text = PathFor(appid);
		if (!File.Exists(text))
		{
			return null;
		}
		try
		{
			if (new FileInfo(text).Length == 9816 && IsHeaderCapsulePlaceholder(File.ReadAllBytes(text)))
			{
				File.Delete(text);
				return null;
			}
		}
		catch
		{
		}
		return text;
	}

	public bool IsKnownMissing(long appid)
	{
		return _noCover.ContainsKey(appid);
	}

	public void MarkMissing(long appid)
	{
		_noCover[appid] = 0;
	}

	public Task<string?> EnsureAsync(long appid, string remoteUrl, CancellationToken ct = default(CancellationToken))
	{
		string path = PathFor(appid);
		if (File.Exists(path))
		{
			return Task.FromResult(path);
		}
		if (string.IsNullOrWhiteSpace(remoteUrl))
		{
			return Task.FromResult<string>(null);
		}
		return _inFlight.GetOrAdd(appid, (long _) => DownloadAsync(appid, path, remoteUrl, ct));
	}

	private async Task<string?> DownloadAsync(long appid, string path, string remoteUrl, CancellationToken ct)
	{
		_ = 2;
		try
		{
			byte[] bytes = await _http.GetByteArrayAsync(remoteUrl, ct);
			if (bytes.Length < 512 || !IsJpeg(bytes))
			{
				return null;
			}
			if (IsHeaderCapsulePlaceholder(bytes))
			{
				return null;
			}
			await _ioGate.WaitAsync(ct);
			try
			{
				Directory.CreateDirectory(CoversDir);
				await File.WriteAllBytesAsync(path, bytes, ct);
			}
			finally
			{
				_ioGate.Release();
			}
			return path;
		}
		catch
		{
			return null;
		}
		finally
		{
			_inFlight.TryRemove(appid, out Task<string> _);
		}
	}
}
