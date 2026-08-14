using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "8.0.14.26413")]
internal sealed class _003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__AddAppIdRegex_3 : Regex
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
				if (num <= inputSpan.Length - 11)
				{
					int num2 = inputSpan.Slice(num).IndexOf("addappid", StringComparison.OrdinalIgnoreCase);
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
				int num3 = 0;
				int num4 = 0;
				int num5 = 0;
				int num6 = 0;
				int num7 = 0;
				int num8 = 0;
				int num9 = 0;
				int arg = 0;
				int arg2 = 0;
				int arg3 = 0;
				int arg4 = 0;
				int num10 = 0;
				int num11 = 0;
				int arg5 = 0;
				int arg6 = 0;
				int arg7 = 0;
				int arg8 = 0;
				int num12 = 0;
				int num13 = 0;
				int num14 = 0;
				int pos = 0;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				if ((uint)span.Length < 8u || !span.StartsWith("addappid", StringComparison.OrdinalIgnoreCase))
				{
					UncaptureUntil(0);
					return false;
				}
				int i;
				for (i = 8; (uint)i < (uint)span.Length && char.IsWhiteSpace(span[i]); i++)
				{
				}
				span = span.Slice(i);
				num += i;
				if (span.IsEmpty || span[0] != '(')
				{
					UncaptureUntil(0);
					return false;
				}
				int j;
				for (j = 1; (uint)j < (uint)span.Length && char.IsWhiteSpace(span[j]); j++)
				{
				}
				span = span.Slice(j);
				num += j;
				num2 = num;
				num6 = num;
				int k;
				for (k = 0; (uint)k < (uint)span.Length && char.IsDigit(span[k]); k++)
				{
				}
				if (k == 0)
				{
					UncaptureUntil(0);
					return false;
				}
				span = span.Slice(k);
				num += k;
				num7 = num;
				num6++;
				while (true)
				{
					num3 = Crawlpos();
					Capture(1, num2, num);
					num8 = num;
					int l;
					for (l = 0; (uint)l < (uint)span.Length && char.IsWhiteSpace(span[l]); l++)
					{
					}
					span = span.Slice(l);
					num += l;
					num9 = num;
					while (true)
					{
						num4 = Crawlpos();
						num12 = 0;
						while (true)
						{
							_003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
							num12++;
							if (!span.IsEmpty && span[0] == ',')
							{
								int m;
								for (m = 1; (uint)m < (uint)span.Length && char.IsWhiteSpace(span[m]); m++)
								{
								}
								span = span.Slice(m);
								num += m;
								arg = num;
								int n;
								for (n = 0; (uint)n < (uint)span.Length && char.IsDigit(span[n]); n++)
								{
								}
								if (n != 0)
								{
									span = span.Slice(n);
									num += n;
									arg2 = num;
									arg++;
									goto IL_0320;
								}
							}
							goto IL_0544;
							IL_03c3:
							_003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.StackPush(ref runstack, ref pos, arg3, arg4, Crawlpos());
							num13 = 0;
							while (true)
							{
								_003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
								num13++;
								if (span.IsEmpty || span[0] != ',')
								{
									break;
								}
								int num15;
								for (num15 = 1; (uint)num15 < (uint)span.Length && char.IsWhiteSpace(span[num15]); num15++)
								{
								}
								span = span.Slice(num15);
								num += num15;
								if (span.IsEmpty || span[0] != '"')
								{
									break;
								}
								num++;
								span = inputSpan.Slice(num);
								int start2 = num;
								int num16 = span.IndexOf('"');
								if (num16 < 0)
								{
									num16 = span.Length;
								}
								span = span.Slice(num16);
								num += num16;
								Capture(2, start2, num);
								if (span.IsEmpty || span[0] != '"')
								{
									break;
								}
								num++;
								span = inputSpan.Slice(num);
								if (num13 == 0)
								{
									continue;
								}
								goto IL_050b;
							}
							goto IL_04d1;
							IL_0544:
							if (--num12 < 0)
							{
								break;
							}
							num = runstack[--pos];
							UncaptureUntil(runstack[--pos]);
							span = inputSpan.Slice(num);
							goto IL_0596;
							IL_050b:
							_003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.StackPush(ref runstack, ref pos, num13);
							if (num12 == 0)
							{
								continue;
							}
							goto IL_0596;
							IL_0596:
							int num17;
							for (num17 = 0; (uint)num17 < (uint)span.Length && char.IsWhiteSpace(span[num17]); num17++)
							{
							}
							span = span.Slice(num17);
							num += num17;
							if (!span.IsEmpty && span[0] == ')')
							{
								num++;
								span = inputSpan.Slice(num);
								num10 = num;
								int num18 = span.IndexOfAnyExcept('\t', ' ');
								if (num18 < 0)
								{
									num18 = span.Length;
								}
								span = span.Slice(num18);
								num += num18;
								num11 = num;
								while (true)
								{
									num5 = Crawlpos();
									num14 = 0;
									while (true)
									{
										_003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
										num14++;
										if (span.StartsWith("--"))
										{
											num += 2;
											span = inputSpan.Slice(num);
											arg5 = num;
											int num19 = span.IndexOfAnyExcept('\t', ' ');
											if (num19 < 0)
											{
												num19 = span.Length;
											}
											span = span.Slice(num19);
											num += num19;
											arg6 = num;
											goto IL_071d;
										}
										goto IL_081a;
										IL_071d:
										_003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.StackPush(ref runstack, ref pos, arg5, arg6, Crawlpos());
										int num20 = num;
										arg8 = num;
										goto IL_078b;
										IL_078b:
										arg7 = Crawlpos();
										_003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.StackPush(ref runstack, ref pos, arg8, arg7);
										Capture(3, num20, num);
										_003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.StackPush(ref runstack, ref pos, num20);
										int num21 = span.IndexOfAnyExcept('\t', ' ');
										if (num21 < 0)
										{
											num21 = span.Length;
										}
										span = span.Slice(num21);
										num += num21;
										if (num14 == 0)
										{
											continue;
										}
										goto IL_086f;
										IL_086f:
										if (num < inputSpan.Length - 1 || ((uint)num < (uint)inputSpan.Length && inputSpan[num] != '\n'))
										{
											if (_003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.s_hasTimeout)
											{
												CheckTimeout();
											}
											if (num14 != 0)
											{
												num20 = runstack[--pos];
												_003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.StackPop(runstack, ref pos, out arg7, out arg8);
												UncaptureUntil(arg7);
												if (_003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.s_hasTimeout)
												{
													CheckTimeout();
												}
												num = arg8;
												span = inputSpan.Slice(num);
												if (span.IsEmpty || span[0] == '\n')
												{
													UncaptureUntil(runstack[--pos]);
													_003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.StackPop(runstack, ref pos, out arg6, out arg5);
													if (_003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.s_hasTimeout)
													{
														CheckTimeout();
													}
													if (arg5 < arg6)
													{
														num = --arg6;
														span = inputSpan.Slice(num);
														goto IL_071d;
													}
													goto IL_081a;
												}
												num++;
												span = inputSpan.Slice(num);
												arg8 = num;
												goto IL_078b;
											}
											break;
										}
										runtextpos = num;
										Capture(0, start, num);
										return true;
										IL_081a:
										if (--num14 < 0)
										{
											break;
										}
										num = runstack[--pos];
										UncaptureUntil(runstack[--pos]);
										span = inputSpan.Slice(num);
										goto IL_086f;
									}
									UncaptureUntil(num5);
									if (_003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.s_hasTimeout)
									{
										CheckTimeout();
									}
									if (num10 >= num11)
									{
										break;
									}
									num = --num11;
									span = inputSpan.Slice(num);
								}
							}
							if (_003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.s_hasTimeout)
							{
								CheckTimeout();
							}
							if (num12 != 0)
							{
								num13 = runstack[--pos];
								if (_003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.s_hasTimeout)
								{
									CheckTimeout();
								}
								goto IL_04d1;
							}
							break;
							IL_0320:
							_003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.StackPush(ref runstack, ref pos, arg, arg2, Crawlpos());
							arg3 = num;
							int num22;
							for (num22 = 0; (uint)num22 < (uint)span.Length && char.IsWhiteSpace(span[num22]); num22++)
							{
							}
							span = span.Slice(num22);
							num += num22;
							arg4 = num;
							goto IL_03c3;
							IL_04d1:
							if (--num13 < 0)
							{
								UncaptureUntil(runstack[--pos]);
								_003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.StackPop(runstack, ref pos, out arg4, out arg3);
								if (_003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.s_hasTimeout)
								{
									CheckTimeout();
								}
								if (arg3 >= arg4)
								{
									UncaptureUntil(runstack[--pos]);
									_003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.StackPop(runstack, ref pos, out arg2, out arg);
									if (_003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.s_hasTimeout)
									{
										CheckTimeout();
									}
									if (arg < arg2)
									{
										num = --arg2;
										span = inputSpan.Slice(num);
										goto IL_0320;
									}
									goto IL_0544;
								}
								num = --arg4;
								span = inputSpan.Slice(num);
								goto IL_03c3;
							}
							num = runstack[--pos];
							UncaptureUntil(runstack[--pos]);
							span = inputSpan.Slice(num);
							goto IL_050b;
						}
						UncaptureUntil(num4);
						if (_003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						if (num8 >= num9)
						{
							break;
						}
						num = --num9;
						span = inputSpan.Slice(num);
					}
					UncaptureUntil(num3);
					if (_003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (num6 >= num7)
					{
						break;
					}
					num = --num7;
					span = inputSpan.Slice(num);
				}
				UncaptureUntil(0);
				return false;
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

	internal static readonly _003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__AddAppIdRegex_3 Instance = new _003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__AddAppIdRegex_3();

	private _003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__AddAppIdRegex_3()
	{
		pattern = "addappid\\s*\\(\\s*(\\d+)\\s*(?:,\\s*\\d+\\s*(?:,\\s*\"([^\"]*)\")?)?\\s*\\)[ \\t]*(?:--[ \\t]*(.*?)[ \\t]*)?$";
		roptions = RegexOptions.IgnoreCase;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EFA13318A2EB302DB38819B77E6B38CEE5154766BE901B11312A85629F3DFB7952__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 4;
	}
}
