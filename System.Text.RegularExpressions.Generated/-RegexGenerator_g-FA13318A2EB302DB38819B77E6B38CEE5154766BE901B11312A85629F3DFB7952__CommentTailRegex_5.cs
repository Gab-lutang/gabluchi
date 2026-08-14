using System.CodeDom.Compiler;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "8.0.14.26413")]
internal sealed class _003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__CommentTailRegex_5 : Regex
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
				if (num <= inputSpan.Length - 3)
				{
					ReadOnlySpan<char> span = inputSpan.Slice(num);
					int num2 = span.IndexOf('(');
					if (num2 >= 0)
					{
						int num3 = num2 - 1;
						while ((uint)num3 < (uint)span.Length && char.IsWhiteSpace(span[num3]))
						{
							num3--;
						}
						runtextpos = num + num3 + 1;
						runtrackpos = num + num2;
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
				int num3 = 0;
				int num4 = 0;
				int num5 = 0;
				int num6 = 0;
				int num7 = 0;
				ReadOnlySpan<char> readOnlySpan = inputSpan.Slice(num);
				num = runtrackpos;
				readOnlySpan = inputSpan.Slice(num);
				if (runtextpos < num)
				{
					runtextpos = num;
				}
				if (readOnlySpan.IsEmpty || readOnlySpan[0] != '(')
				{
					return false;
				}
				num++;
				readOnlySpan = inputSpan.Slice(num);
				int i;
				for (i = 0; (uint)i < (uint)readOnlySpan.Length && char.IsDigit(readOnlySpan[i]); i++)
				{
				}
				if (i == 0)
				{
					return false;
				}
				readOnlySpan = readOnlySpan.Slice(i);
				num += i;
				if (readOnlySpan.IsEmpty || readOnlySpan[0] != ')')
				{
					return false;
				}
				num++;
				readOnlySpan = inputSpan.Slice(num);
				num2 = num;
				int j;
				for (j = 0; (uint)j < (uint)readOnlySpan.Length && char.IsWhiteSpace(readOnlySpan[j]); j++)
				{
				}
				readOnlySpan = readOnlySpan.Slice(j);
				num += j;
				num3 = num;
				while (true)
				{
					num4 = num;
					int k;
					for (k = 0; (uint)k < (uint)readOnlySpan.Length && !char.IsWhiteSpace(readOnlySpan[k]); k++)
					{
					}
					readOnlySpan = readOnlySpan.Slice(k);
					num += k;
					num5 = num;
					while (true)
					{
						num6 = num;
						int l;
						for (l = 0; (uint)l < (uint)readOnlySpan.Length && char.IsWhiteSpace(readOnlySpan[l]); l++)
						{
						}
						readOnlySpan = readOnlySpan.Slice(l);
						num += l;
						num7 = num;
						while (true)
						{
							if (num < inputSpan.Length - 1 || ((uint)num < (uint)inputSpan.Length && inputSpan[num] != '\n'))
							{
								if (_003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.s_hasTimeout)
								{
									CheckTimeout();
								}
								if (num6 >= num7)
								{
									break;
								}
								num = --num7;
								readOnlySpan = inputSpan.Slice(num);
								continue;
							}
							runtextpos = num;
							Capture(0, start, num);
							return true;
						}
						if (_003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						if (num4 >= num5)
						{
							break;
						}
						num = --num5;
						readOnlySpan = inputSpan.Slice(num);
					}
					if (_003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (num2 >= num3)
					{
						break;
					}
					num = --num3;
					readOnlySpan = inputSpan.Slice(num);
				}
				return false;
			}
		}

		protected override RegexRunner CreateInstance()
		{
			return new Runner();
		}
	}

	internal static readonly _003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__CommentTailRegex_5 Instance = new _003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__CommentTailRegex_5();

	private _003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__CommentTailRegex_5()
	{
		pattern = "\\s*\\(\\d+\\)\\s*\\S*\\s*$";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 1;
	}
}
