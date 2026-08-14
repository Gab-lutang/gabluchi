using System.CodeDom.Compiler;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "8.0.14.26413")]
internal sealed class _003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__KeyFormatRegex_2 : Regex
{
	private sealed class RunnerFactory : RegexRunnerFactory
	{
		private sealed class Runner : RegexRunner
		{
			protected override void Scan(ReadOnlySpan<char> inputSpan)
			{
				if (TryFindNextPossibleStartingPosition(inputSpan) && !TryMatchAtCurrentPosition(inputSpan))
				{
					runtextpos = inputSpan.Length;
				}
			}

			private bool TryFindNextPossibleStartingPosition(ReadOnlySpan<char> inputSpan)
			{
				int num = runtextpos;
				if (num <= inputSpan.Length - 100 && num == 0)
				{
					return true;
				}
				runtextpos = inputSpan.Length;
				return false;
			}

			private bool TryMatchAtCurrentPosition(ReadOnlySpan<char> inputSpan)
			{
				int num = runtextpos;
				int start = num;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				if (num != 0)
				{
					return false;
				}
				if ((uint)span.Length < 100u || !span.StartsWith("smm_"))
				{
					return false;
				}
				if (span.Slice(4, 96).IndexOfAnyExcept(_003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.s_asciiHexDigitsLower) >= 0)
				{
					return false;
				}
				if (101 < span.Length || (100 < span.Length && span[100] != '\n'))
				{
					return false;
				}
				Capture(0, start, runtextpos = num + 100);
				return true;
			}
		}

		protected override RegexRunner CreateInstance()
		{
			return new Runner();
		}
	}

	internal static readonly _003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__KeyFormatRegex_2 Instance = new _003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__KeyFormatRegex_2();

	private _003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__KeyFormatRegex_2()
	{
		pattern = "^smm_[0-9a-f]{96}$";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 1;
	}
}
