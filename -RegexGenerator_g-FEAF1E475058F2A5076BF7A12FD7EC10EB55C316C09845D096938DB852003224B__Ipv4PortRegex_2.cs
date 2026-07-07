using System.CodeDom.Compiler;
using System.Collections;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.23019")]
internal sealed class _003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Ipv4PortRegex_2 : Regex
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
				if (num <= inputSpan.Length - 9)
				{
					int num2 = inputSpan.Slice(num).IndexOfAnyDigit();
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
				ReadOnlySpan<char> readOnlySpan = inputSpan.Slice(num);
				readOnlySpan = inputSpan.Slice(num);
				int num10 = num;
				if (_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				if ((uint)(num - 1) < inputSpan.Length && char.IsDigit(inputSpan[num - 1]))
				{
					num--;
					UncaptureUntil(0);
					return false;
				}
				num = num10;
				readOnlySpan = inputSpan.Slice(num);
				readOnlySpan = inputSpan.Slice(num);
				int num11 = num;
				if (_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.s_hasTimeout)
				{
					CheckTimeout();
				}
				if ((uint)(num - 1) < inputSpan.Length && inputSpan[num - 1] == '.')
				{
					num--;
					UncaptureUntil(0);
					return false;
				}
				num = num11;
				readOnlySpan = inputSpan.Slice(num);
				num2 = num;
				int i;
				for (i = 0; i < 3 && (uint)i < (uint)readOnlySpan.Length && char.IsDigit(readOnlySpan[i]); i++)
				{
				}
				if (i == 0)
				{
					UncaptureUntil(0);
					return false;
				}
				readOnlySpan = readOnlySpan.Slice(i);
				num += i;
				if (readOnlySpan.IsEmpty || readOnlySpan[0] != '.')
				{
					UncaptureUntil(0);
					return false;
				}
				num++;
				readOnlySpan = inputSpan.Slice(num);
				int j;
				for (j = 0; j < 3 && (uint)j < (uint)readOnlySpan.Length && char.IsDigit(readOnlySpan[j]); j++)
				{
				}
				if (j == 0)
				{
					UncaptureUntil(0);
					return false;
				}
				readOnlySpan = readOnlySpan.Slice(j);
				num += j;
				if (readOnlySpan.IsEmpty || readOnlySpan[0] != '.')
				{
					UncaptureUntil(0);
					return false;
				}
				num++;
				readOnlySpan = inputSpan.Slice(num);
				int k;
				for (k = 0; k < 3 && (uint)k < (uint)readOnlySpan.Length && char.IsDigit(readOnlySpan[k]); k++)
				{
				}
				if (k == 0)
				{
					UncaptureUntil(0);
					return false;
				}
				readOnlySpan = readOnlySpan.Slice(k);
				num += k;
				if (readOnlySpan.IsEmpty || readOnlySpan[0] != '.')
				{
					UncaptureUntil(0);
					return false;
				}
				num++;
				readOnlySpan = inputSpan.Slice(num);
				num6 = num;
				int l;
				for (l = 0; l < 3 && (uint)l < (uint)readOnlySpan.Length && char.IsDigit(readOnlySpan[l]); l++)
				{
				}
				if (l == 0)
				{
					UncaptureUntil(0);
					return false;
				}
				readOnlySpan = readOnlySpan.Slice(l);
				num += l;
				num7 = num;
				num6++;
				while (true)
				{
					num4 = Crawlpos();
					Capture(1, num2, num);
					readOnlySpan = inputSpan.Slice(num);
					int num12 = num;
					if (_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (readOnlySpan.IsEmpty || readOnlySpan[0] != '.')
					{
						num = num12;
						readOnlySpan = inputSpan.Slice(num);
						readOnlySpan = inputSpan.Slice(num);
						int num13 = num;
						if (_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						if (readOnlySpan.IsEmpty || !char.IsDigit(readOnlySpan[0]))
						{
							num = num13;
							readOnlySpan = inputSpan.Slice(num);
							num8 = num;
							int m;
							for (m = 0; (uint)m < (uint)readOnlySpan.Length; m++)
							{
								char c;
								if ((((c = readOnlySpan[m]) < '\u0080') ? ("㸀\0\u0001Ѐ\0\0\0\0"[(int)c >> 4] & (1 << (c & 0xF))) : (RegexRunner.CharInClass(c, "\0\u0002\u0001:;d") ? 1 : 0)) == 0)
								{
									break;
								}
							}
							if (m != 0)
							{
								readOnlySpan = readOnlySpan.Slice(m);
								num += m;
								num9 = num;
								num8++;
								while (true)
								{
									num5 = Crawlpos();
									num3 = num;
									int n;
									for (n = 0; n < 5 && (uint)n < (uint)readOnlySpan.Length && char.IsDigit(readOnlySpan[n]); n++)
									{
									}
									if (n != 0)
									{
										readOnlySpan = readOnlySpan.Slice(n);
										num += n;
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

	internal static readonly _003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Ipv4PortRegex_2 Instance = new _003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Ipv4PortRegex_2();

	private _003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Ipv4PortRegex_2()
	{
		pattern = "(?<!\\d)(?<!\\.)(?<host>\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3})(?!\\.)(?!\\d)[\\s:]+(?<port>\\d{1,5})\\b";
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
