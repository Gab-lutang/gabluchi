using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace GabLuchi.Services;

public sealed class RuntimeCheckerService
{
	private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);

	private IReadOnlyDictionary<string, IReadOnlyList<Version>>? _runtimes;

	private bool? _registrySharedFxPresent;

	public bool IsDotNet9Available => HasRuntime("Microsoft.NETCore.App", 9, 0);

	public bool HasRuntime(string framework, int major, int minor = 0)
	{
		if (_runtimes == null)
		{
			return false;
		}
		if (_runtimes.TryGetValue(framework, out IReadOnlyList<Version>? versions))
		{
			return versions.Any(v => v.Major > major || (v.Major == major && v.Minor >= minor));
		}
		return false;
	}

	public bool RegistrySharedFxPresent => _registrySharedFxPresent ?? false;

	public async Task<bool> RefreshAsync(CancellationToken ct = default)
	{
		await _gate.WaitAsync(ct);
		try
		{
			_runtimes = await DetectRuntimesAsync(ct);
			_registrySharedFxPresent = DetectRegistrySharedFx();
			return true;
		}
		finally
		{
			_gate.Release();
		}
	}

	private static async Task<IReadOnlyDictionary<string, IReadOnlyList<Version>>> DetectRuntimesAsync(CancellationToken ct)
	{
		Dictionary<string, IReadOnlyList<Version>> result = new Dictionary<string, IReadOnlyList<Version>>(StringComparer.OrdinalIgnoreCase);
		try
		{
			ProcessStartInfo startInfo = new ProcessStartInfo("dotnet", "--list-runtimes")
			{
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true
			};
			using Process? proc = Process.Start(startInfo);
			if (proc == null)
			{
				return result;
			}
			string output = await proc.StandardOutput.ReadToEndAsync(ct);
			await proc.WaitForExitAsync(ct);
			foreach (string line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
			{
				string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length < 2)
				{
					continue;
				}
				string name = parts[0];
				if (Version.TryParse(parts[1], out Version? version))
				{
					if (!result.TryGetValue(name, out IReadOnlyList<Version>? existing))
					{
						List<Version> list = new List<Version>();
						list.Add(version);
						result[name] = list;
					}
					else
					{
						((List<Version>)existing).Add(version);
					}
				}
			}
		}
		catch
		{
		}
		return result;
	}

	private static bool DetectRegistrySharedFx()
	{
		try
		{
			using RegistryKey? baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
			using RegistryKey? key = baseKey?.OpenSubKey(@"SOFTWARE\dotnet\Setup\InstalledVersions\x64");
			if (key == null)
			{
				return false;
			}
			return key.GetSubKeyNames().Contains("sharedfx", StringComparer.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}
}