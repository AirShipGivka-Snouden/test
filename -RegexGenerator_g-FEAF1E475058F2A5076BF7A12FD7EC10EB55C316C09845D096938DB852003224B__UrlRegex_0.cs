using System.CodeDom.Compiler;
using System.Collections;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.23019")]
internal sealed class _003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__UrlRegex_0 : Regex
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
					if (_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
				}
			}

			private bool TryFindNextPossibleStartingPosition(ReadOnlySpan<char> inputSpan)
			{
				int num = runtextpos;
				if (num <= inputSpan.Length - 8)
				{
					int num2 = inputSpan.Slice(num).IndexOfAny(_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.s_indexOfAnyStrings_OrdinalIgnoreCase_5A4D9B017D3E6477E32C9603AB4180D9672961E82A4AB9ED0C76157EDE808508);
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
				int capturePosition = 0;
				int num6 = 0;
				int num7 = 0;
				int num8 = 0;
				int num9 = 0;
				int num10 = 0;
				int capturePosition2 = 0;
				int num11 = 0;
				int arg = 0;
				int num12 = 0;
				int num13 = 0;
				int num14 = 0;
				int num15 = 0;
				int num16 = 0;
				int pos = 0;
				int num17 = 0;
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				num10 = num;
				num7 = num;
				num5 = Crawlpos();
				if ((uint)span.Length < 4u || !span.StartsWith("http".AsSpan(), StringComparison.OrdinalIgnoreCase))
				{
					goto IL_00b7;
				}
				if ((uint)span.Length > 4u && (span[4] | 0x20) == 115)
				{
					span = span.Slice(1);
					num++;
				}
				num2 = 0;
				num += 4;
				span = inputSpan.Slice(num);
				goto IL_0220;
				IL_0220:
				char c;
				while (true)
				{
					if (span.StartsWith("://".AsSpan()))
					{
						num += 3;
						span = inputSpan.Slice(num);
						num14 = 0;
						while (true)
						{
							_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
							num14++;
							num11 = num;
							int i;
							for (i = 0; (uint)i < (uint)span.Length; i++)
							{
								if ((((c = span[i]) < '\u0080') ? ("쇿\uffff\ufffeﯿ\ufffe\uffff\uffff\uffff"[(int)c >> 4] & (1 << (c & 0xF))) : (RegexRunner.CharInClass(c, "\u0001\u0004\u0001:;@Ad") ? 1 : 0)) == 0)
								{
									break;
								}
							}
							if (i != 0)
							{
								span = span.Slice(i);
								num += i;
								arg = num;
								num11++;
								goto IL_0322;
							}
							goto IL_046e;
							IL_0422:
							num15 = runstack[--pos];
							if (_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.s_hasTimeout)
							{
								CheckTimeout();
							}
							goto IL_03d7;
							IL_03d7:
							if (--num15 < 0)
							{
								UncaptureUntil(runstack[--pos]);
								_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.StackPop(runstack, ref pos, out arg, out num11);
								if (_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.s_hasTimeout)
								{
									CheckTimeout();
								}
								if (num11 < arg)
								{
									num = --arg;
									span = inputSpan.Slice(num);
									goto IL_0322;
								}
								goto IL_046e;
							}
							num = runstack[--pos];
							UncaptureUntil(runstack[--pos]);
							span = inputSpan.Slice(num);
							goto IL_0411;
							IL_0546:
							num = num9;
							span = inputSpan.Slice(num);
							UncaptureUntil(num6);
							num12 = num;
							int num18 = span.IndexOfAnyExcept(_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.s_asciiLettersAndDigitsAndDashDotKelvinSign);
							if (num18 < 0)
							{
								num18 = span.Length;
							}
							if (num18 == 0)
							{
								goto IL_04aa;
							}
							span = span.Slice(num18);
							num += num18;
							num13 = num;
							num12++;
							goto IL_05cc;
							IL_046e:
							if (--num14 < 0)
							{
								break;
							}
							num = runstack[--pos];
							UncaptureUntil(runstack[--pos]);
							span = inputSpan.Slice(num);
							goto IL_04c3;
							IL_04aa:
							if (_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.s_hasTimeout)
							{
								CheckTimeout();
							}
							if (num14 == 0)
							{
								break;
							}
							goto IL_0422;
							IL_04c3:
							num9 = num;
							num6 = Crawlpos();
							if (!span.IsEmpty && span[0] == '[')
							{
								int num19 = span.Slice(1).IndexOf(']');
								if (num19 < 0)
								{
									num19 = span.Length - 1;
								}
								if (num19 != 0)
								{
									span = span.Slice(num19);
									num += num19;
									if ((uint)span.Length >= 2u && span[1] == ']')
									{
										num4 = 0;
										num += 2;
										span = inputSpan.Slice(num);
										goto IL_05f2;
									}
								}
							}
							goto IL_0546;
							IL_0411:
							_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.StackPush(ref runstack, ref pos, num15);
							if (span.IsEmpty || span[0] != '@')
							{
								goto IL_0422;
							}
							num++;
							span = inputSpan.Slice(num);
							if (num14 == 0)
							{
								continue;
							}
							goto IL_04c3;
							IL_0322:
							_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.StackPush(ref runstack, ref pos, num11, arg, Crawlpos());
							num15 = 0;
							while (true)
							{
								_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
								num15++;
								if (span.IsEmpty || span[0] != ':')
								{
									break;
								}
								int j;
								for (j = 1; (uint)j < (uint)span.Length; j++)
								{
									if ((((c = span[j]) < '\u0080') ? ("쇿\uffff\ufffe\uffff\ufffe\uffff\uffff\uffff"[(int)c >> 4] & (1 << (c & 0xF))) : (RegexRunner.CharInClass(c, "\u0001\u0002\u0001@Ad") ? 1 : 0)) == 0)
									{
										break;
									}
								}
								span = span.Slice(j);
								num += j;
								if (num15 == 0)
								{
									continue;
								}
								goto IL_0411;
							}
							goto IL_03d7;
							IL_05f2:
							while (true)
							{
								num17 = pos;
								num16 = 0;
								while (true)
								{
									_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
									num16++;
									if (!span.IsEmpty && span[0] == ':')
									{
										num++;
										span = inputSpan.Slice(num);
										int k;
										for (k = 0; k < 5 && (uint)k < (uint)span.Length && char.IsDigit(span[k]); k++)
										{
										}
										if (k != 0)
										{
											span = span.Slice(k);
											num += k;
											if (num16 == 0)
											{
												continue;
											}
											goto IL_06ba;
										}
									}
									if (--num16 < 0)
									{
										break;
									}
									num = runstack[--pos];
									UncaptureUntil(runstack[--pos]);
									span = inputSpan.Slice(num);
									goto IL_06ba;
									IL_06ba:
									pos = num17;
									Capture(1, num10, num);
									runtextpos = num;
									Capture(0, start, num);
									return true;
								}
								if (_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.s_hasTimeout)
								{
									CheckTimeout();
								}
								if (num4 == 0)
								{
									break;
								}
								if (num4 != 1)
								{
									continue;
								}
								goto IL_059c;
							}
							goto IL_0546;
							IL_05cc:
							capturePosition2 = Crawlpos();
							num4 = 1;
							goto IL_05f2;
							IL_059c:
							UncaptureUntil(capturePosition2);
							if (_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.s_hasTimeout)
							{
								CheckTimeout();
							}
							if (num12 >= num13)
							{
								goto IL_04aa;
							}
							num = --num13;
							span = inputSpan.Slice(num);
							goto IL_05cc;
						}
					}
					if (_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (num2 == 0)
					{
						break;
					}
					if (num2 != 1)
					{
						continue;
					}
					goto IL_01e6;
				}
				goto IL_00b7;
				IL_019b:
				num = num8;
				span = inputSpan.Slice(num);
				UncaptureUntil(capturePosition);
				if ((uint)span.Length < 5u || (span[4] | 0x20) != 115)
				{
					UncaptureUntil(0);
					return false;
				}
				num3 = 1;
				num += 5;
				span = inputSpan.Slice(num);
				goto IL_0205;
				IL_0205:
				num2 = 1;
				goto IL_0220;
				IL_01e6:
				if (_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				if (num3 == 0)
				{
					goto IL_019b;
				}
				if (num3 == 1)
				{
					UncaptureUntil(0);
					return false;
				}
				goto IL_0205;
				IL_00b7:
				num = num7;
				span = inputSpan.Slice(num);
				UncaptureUntil(num5);
				if ((uint)span.Length < 4u || !span.StartsWith("soc".AsSpan(), StringComparison.OrdinalIgnoreCase) || (((c = span[3]) | 0x20) != 107 && c != 'K'))
				{
					UncaptureUntil(0);
					return false;
				}
				num8 = num;
				capturePosition = Crawlpos();
				if ((uint)span.Length < 5u || (span[4] | 0x20) != 115)
				{
					goto IL_019b;
				}
				if ((uint)span.Length > 5u && span[5] == '5')
				{
					span = span.Slice(1);
					num++;
				}
				if ((uint)span.Length > 5u && (span[5] | 0x20) == 104)
				{
					span = span.Slice(1);
					num++;
				}
				num3 = 0;
				num += 5;
				span = inputSpan.Slice(num);
				goto IL_0205;
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				void UncaptureUntil(int num20)
				{
					while (Crawlpos() > num20)
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

	internal static readonly _003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__UrlRegex_0 Instance = new _003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__UrlRegex_0();

	private _003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__UrlRegex_0()
	{
		pattern = "(?<url>(?:https?|socks5?h?|socks)://(?:[^:@\\s]+(?::[^@\\s]*)?@)?(?:\\[[^\\]]+\\]|[a-zA-Z0-9\\.\\-]+)(?::\\d{1,5})?)";
		roptions = RegexOptions.IgnoreCase;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		base.CapNames = new Hashtable
		{
			{ "0", 0 },
			{ "url", 1 }
		};
		capslist = new string[2] { "0", "url" };
		capsize = 2;
	}
}
