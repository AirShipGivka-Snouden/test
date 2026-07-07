using System.CodeDom.Compiler;
using System.Collections;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.23019")]
internal sealed class _003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__HostPortRegex_3 : Regex
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
				if (num <= inputSpan.Length - 5)
				{
					int num2 = inputSpan.Slice(num).IndexOfAny(_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.s_asciiLettersAndDigits);
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
				int arg = 0;
				int arg2 = 0;
				int num6 = 0;
				int num7 = 0;
				int num8 = 0;
				int num9 = 0;
				int num10 = 0;
				int num11 = 0;
				int pos = 0;
				ReadOnlySpan<char> readOnlySpan = inputSpan.Slice(num);
				if (!_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.IsPreWordCharBoundary(inputSpan, num))
				{
					UncaptureUntil(0);
					return false;
				}
				num2 = num;
				num10 = 0;
				while (true)
				{
					_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
					num10++;
					if (!readOnlySpan.IsEmpty && char.IsAsciiLetterOrDigit(readOnlySpan[0]))
					{
						num++;
						readOnlySpan = inputSpan.Slice(num);
						num11 = 0;
						goto IL_009a;
					}
					goto IL_0273;
					IL_0111:
					UncaptureUntil(runstack[--pos]);
					_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.StackPop(runstack, ref pos, out arg2, out arg);
					if (_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (arg < arg2 && (arg2 = inputSpan.Slice(arg, arg2 - arg).LastIndexOfAny(_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.s_asciiLettersAndDigits)) >= 0)
					{
						arg2 += arg;
						num = arg2;
						readOnlySpan = inputSpan.Slice(num);
						goto IL_017b;
					}
					if (--num11 >= 0)
					{
						num = runstack[--pos];
						UncaptureUntil(runstack[--pos]);
						readOnlySpan = inputSpan.Slice(num);
						goto IL_021a;
					}
					goto IL_0273;
					IL_009a:
					_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
					num11++;
					arg = num;
					int i;
					for (i = 0; i < 61 && (uint)i < (uint)readOnlySpan.Length; i++)
					{
						char c;
						if ((c = readOnlySpan[i]) >= '{')
						{
							break;
						}
						if (("\0\0\u2000Ͽ\ufffe߿\ufffe߿"[(int)c >> 4] & (1 << (c & 0xF))) == 0)
						{
							break;
						}
					}
					readOnlySpan = readOnlySpan.Slice(i);
					num += i;
					arg2 = num;
					goto IL_017b;
					IL_0273:
					if (--num10 < 0)
					{
						UncaptureUntil(0);
						return false;
					}
					num = runstack[--pos];
					UncaptureUntil(runstack[--pos]);
					readOnlySpan = inputSpan.Slice(num);
					if (num10 == 0)
					{
						UncaptureUntil(0);
						return false;
					}
					if (!readOnlySpan.IsEmpty && char.IsAsciiLetter(readOnlySpan[0]))
					{
						num++;
						readOnlySpan = inputSpan.Slice(num);
						num6 = num;
						int j;
						for (j = 0; j < 61 && (uint)j < (uint)readOnlySpan.Length; j++)
						{
							char c;
							if ((c = readOnlySpan[j]) >= '{')
							{
								break;
							}
							if (("\0\0\u2000Ͽ\ufffe߿\ufffe߿"[(int)c >> 4] & (1 << (c & 0xF))) == 0)
							{
								break;
							}
						}
						readOnlySpan = readOnlySpan.Slice(j);
						num += j;
						num7 = num;
						while (true)
						{
							num4 = Crawlpos();
							if (!readOnlySpan.IsEmpty && char.IsAsciiLetterOrDigit(readOnlySpan[0]))
							{
								readOnlySpan = readOnlySpan.Slice(1);
								num++;
							}
							Capture(1, num2, num);
							num8 = num;
							int k;
							for (k = 0; (uint)k < (uint)readOnlySpan.Length; k++)
							{
								char c;
								if ((((c = readOnlySpan[k]) < '\u0080') ? ("㸀\0\u0001Ѐ\0\0\0\0"[(int)c >> 4] & (1 << (c & 0xF))) : (RegexRunner.CharInClass(c, "\0\u0002\u0001:;d") ? 1 : 0)) == 0)
								{
									break;
								}
							}
							if (k != 0)
							{
								readOnlySpan = readOnlySpan.Slice(k);
								num += k;
								num9 = num;
								num8++;
								while (true)
								{
									num5 = Crawlpos();
									num3 = num;
									int l;
									for (l = 0; l < 5 && (uint)l < (uint)readOnlySpan.Length && char.IsDigit(readOnlySpan[l]); l++)
									{
									}
									if (l != 0)
									{
										readOnlySpan = readOnlySpan.Slice(l);
										num += l;
										Capture(2, num3, num);
										if (_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.IsPostWordCharBoundary(inputSpan, num))
										{
											runtextpos = num;
											Capture(0, start, num);
											return true;
										}
									}
									UncaptureUntil(num5);
									if (_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.s_hasTimeout)
									{
										CheckTimeout();
									}
									if (num8 >= num9)
									{
										break;
									}
									num = --num9;
									readOnlySpan = inputSpan.Slice(num);
								}
							}
							UncaptureUntil(num4);
							if (_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.s_hasTimeout)
							{
								CheckTimeout();
							}
							if (num6 >= num7)
							{
								break;
							}
							num = --num7;
							readOnlySpan = inputSpan.Slice(num);
						}
					}
					if (_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (num10 == 0)
					{
						break;
					}
					goto IL_022b;
					IL_021a:
					_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.StackPush(ref runstack, ref pos, num11);
					if (!readOnlySpan.IsEmpty && readOnlySpan[0] == '.')
					{
						num++;
						readOnlySpan = inputSpan.Slice(num);
						continue;
					}
					goto IL_022b;
					IL_022b:
					num11 = runstack[--pos];
					if (_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (num11 != 0)
					{
						goto IL_0111;
					}
					goto IL_0273;
					IL_017b:
					_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.StackPush(ref runstack, ref pos, arg, arg2, Crawlpos());
					if (readOnlySpan.IsEmpty || !char.IsAsciiLetterOrDigit(readOnlySpan[0]))
					{
						goto IL_0111;
					}
					num++;
					readOnlySpan = inputSpan.Slice(num);
					if (num11 == 0)
					{
						goto IL_009a;
					}
					goto IL_021a;
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

	internal static readonly _003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__HostPortRegex_3 Instance = new _003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__HostPortRegex_3();

	private _003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__HostPortRegex_3()
	{
		pattern = "\\b(?<host>(?:[a-zA-Z0-9](?:[a-zA-Z0-9\\-]{0,61}[a-zA-Z0-9])?\\.)+[a-zA-Z][a-zA-Z0-9\\-]{0,61}[a-zA-Z0-9]?)[\\s:]+(?<port>\\d{1,5})\\b";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		base.CapNames = new Hashtable
		{
			{ "0", 0 },
			{ "host", 1 },
			{ "port", 2 }
		};
		capslist = new string[3] { "0", "host", "port" };
		capsize = 3;
	}
}
