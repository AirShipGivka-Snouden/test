using System.CodeDom.Compiler;
using System.Collections;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.23019")]
internal sealed class _003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Ipv4OnlyRegex_5 : Regex
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
				if (num <= inputSpan.Length - 7)
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
				ReadOnlySpan<char> readOnlySpan = inputSpan.Slice(num);
				readOnlySpan = inputSpan.Slice(num);
				int num6 = num;
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
				num = num6;
				readOnlySpan = inputSpan.Slice(num);
				readOnlySpan = inputSpan.Slice(num);
				int num7 = num;
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
				num = num7;
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
				num4 = num;
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
				num5 = num;
				num4++;
				int num9;
				while (true)
				{
					num3 = Crawlpos();
					Capture(1, num2, num);
					readOnlySpan = inputSpan.Slice(num);
					int num8 = num;
					if (_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (readOnlySpan.IsEmpty || readOnlySpan[0] != '.')
					{
						num = num8;
						readOnlySpan = inputSpan.Slice(num);
						readOnlySpan = inputSpan.Slice(num);
						num9 = num;
						if (_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						if (readOnlySpan.IsEmpty || !char.IsDigit(readOnlySpan[0]))
						{
							break;
						}
					}
					UncaptureUntil(num3);
					if (_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					if (num4 >= num5)
					{
						UncaptureUntil(0);
						return false;
					}
					num = --num5;
					readOnlySpan = inputSpan.Slice(num);
				}
				num = num9;
				readOnlySpan = inputSpan.Slice(num);
				runtextpos = num;
				Capture(0, start, num);
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

	internal static readonly _003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Ipv4OnlyRegex_5 Instance = new _003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Ipv4OnlyRegex_5();

	private _003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Ipv4OnlyRegex_5()
	{
		pattern = "(?<!\\d)(?<!\\.)(?<host>\\d{1,3}\\.\\d{1,3}\\.\\d{1,3}\\.\\d{1,3})(?!\\.)(?!\\d)";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		base.CapNames = new Hashtable
		{
			{ "0", 0 },
			{ "host", 1 }
		};
		capslist = new string[2] { "0", "host" };
		capsize = 2;
	}
}
