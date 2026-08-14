using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "8.0.14.26413")]
internal sealed class _003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__PathRegex_7 : Regex
{
	private sealed class RunnerFactory : RegexRunnerFactory
	{
		private sealed class Runner : RegexRunner
		{
			protected override void Scan(ReadOnlySpan<char> inputSpan)
			{
				while (TryFindNextPossibleStartingPosition(inputSpan) && !TryMatchAtCurrentPosition(inputSpan) && runtextpos != inputSpan.Length)
				{
					runtextpos++;
					if (_003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
				}
			}

			private bool TryFindNextPossibleStartingPosition(ReadOnlySpan<char> inputSpan)
			{
				int num = runtextpos;
				if (num <= inputSpan.Length - 9)
				{
					int num2 = inputSpan.Slice(num).IndexOf("\"path\"", StringComparison.OrdinalIgnoreCase);
					if (num2 >= 0)
					{
						runtextpos = num + num2;
						return true;
					}
				}
				runtextpos = inputSpan.Length;
				return false;
			}

			private bool TryMatchAtCurrentPosition(ReadOnlySpan<char> inputSpan)
			{
				int num = runtextpos;
				int start = num;
				int num2 = 0;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				if ((uint)span.Length < 6u || !span.StartsWith("\"path\"", StringComparison.OrdinalIgnoreCase))
				{
					UncaptureUntil(0);
					return false;
				}
				int i;
				for (i = 6; (uint)i < (uint)span.Length && char.IsWhiteSpace(span[i]); i++)
				{
				}
				span = span.Slice(i);
				num += i;
				if (span.IsEmpty || span[0] != '"')
				{
					UncaptureUntil(0);
					return false;
				}
				num++;
				span = inputSpan.Slice(num);
				num2 = num;
				int num3 = span.IndexOf('"');
				if (num3 < 0)
				{
					num3 = span.Length;
				}
				if (num3 == 0)
				{
					UncaptureUntil(0);
					return false;
				}
				span = span.Slice(num3);
				num += num3;
				Capture(1, num2, num);
				if (span.IsEmpty || span[0] != '"')
				{
					UncaptureUntil(0);
					return false;
				}
				Capture(0, start, runtextpos = num + 1);
				return true;
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				void UncaptureUntil(int capturePosition)
				{
					while (Crawlpos() > capturePosition)
					{
						Uncapture();
					}
				}
			}
		}

		protected override RegexRunner CreateInstance()
		{
			return new Runner();
		}
	}

	internal static readonly _003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__PathRegex_7 Instance = new _003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__PathRegex_7();

	private _003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__PathRegex_7()
	{
		pattern = "\"path\"\\s*\"([^\"]+)\"";
		roptions = RegexOptions.IgnoreCase;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 2;
	}
}
