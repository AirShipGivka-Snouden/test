using System.Buffers;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.23019")]
internal static class _003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities
{
	internal static readonly TimeSpan s_defaultTimeout = ((AppContext.GetData("REGEX_DEFAULT_MATCH_TIMEOUT") is TimeSpan timeSpan) ? timeSpan : Regex.InfiniteMatchTimeout);

	internal static readonly bool s_hasTimeout = s_defaultTimeout != Regex.InfiniteMatchTimeout;

	private const int WordCategoriesMask = 262463;

	internal static readonly SearchValues<char> s_asciiExceptDigits = SearchValues.Create("\0\u0001\u0002\u0003\u0004\u0005\u0006\a\b\t\n\v\f\r\u000e\u000f\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001a\u001b\u001c\u001d\u001e\u001f !\"#$%&'()*+,-./:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~\u007f".AsSpan());

	internal static readonly SearchValues<char> s_asciiLettersAndDigits = SearchValues.Create("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".AsSpan());

	internal static readonly SearchValues<char> s_asciiLettersAndDigitsAndDashDotKelvinSign = SearchValues.Create("-.0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzK".AsSpan());

	internal static readonly SearchValues<char> s_ascii_FF077E0000007E000000 = SearchValues.Create("0123456789:ABCDEFabcdef".AsSpan());

	internal static readonly SearchValues<string> s_indexOfAnyStrings_OrdinalIgnoreCase_5A4D9B017D3E6477E32C9603AB4180D9672961E82A4AB9ED0C76157EDE808508;

	private static ReadOnlySpan<byte> WordCharBitmap => new byte[16]
	{
		0, 0, 0, 0, 0, 0, 255, 3, 254, 255,
		255, 135, 254, 255, 255, 7
	};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int IndexOfAnyDigit(this ReadOnlySpan<char> span)
	{
		int num = span.IndexOfAnyExcept(s_asciiExceptDigits);
		if ((uint)num < (uint)span.Length)
		{
			if (char.IsAscii(span[num]))
			{
				return num;
			}
			do
			{
				if (char.IsDigit(span[num]))
				{
					return num;
				}
				num++;
			}
			while ((uint)num < (uint)span.Length);
		}
		return -1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsBoundaryWordChar(char ch)
	{
		ReadOnlySpan<byte> wordCharBitmap = WordCharBitmap;
		int num = (int)ch >> 3;
		if ((uint)num < (uint)wordCharBitmap.Length)
		{
			return (wordCharBitmap[num] & (1 << (ch & 7))) != 0;
		}
		bool flag = (0x4013F & (1 << (int)CharUnicodeInfo.GetUnicodeCategory(ch))) != 0;
		if (!flag)
		{
			bool flag2 = ((ch == '\u200c' || ch == '\u200d') ? true : false);
			flag = flag2;
		}
		return flag;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsPostWordCharBoundary(ReadOnlySpan<char> inputSpan, int index)
	{
		if ((uint)index < (uint)inputSpan.Length)
		{
			return !IsBoundaryWordChar(inputSpan[index]);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsPreWordCharBoundary(ReadOnlySpan<char> inputSpan, int index)
	{
		int num = index - 1;
		if ((uint)num < (uint)inputSpan.Length)
		{
			return !IsBoundaryWordChar(inputSpan[num]);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void StackPop(int[] stack, ref int pos, out int arg0, out int arg1)
	{
		arg0 = stack[--pos];
		arg1 = stack[--pos];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void StackPush(ref int[] stack, ref int pos, int arg0)
	{
		int[] array = stack;
		int num = pos;
		if ((uint)num < (uint)array.Length)
		{
			array[num] = arg0;
			pos++;
		}
		else
		{
			WithResize(ref stack, ref pos, arg0);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		static void WithResize(ref int[] reference, ref int reference2, int arg1)
		{
			Array.Resize(ref reference, reference2 * 2);
			StackPush(ref reference, ref reference2, arg1);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void StackPush(ref int[] stack, ref int pos, int arg0, int arg1)
	{
		int[] array = stack;
		int num = pos;
		if ((uint)(num + 1) < (uint)array.Length)
		{
			array[num] = arg0;
			array[num + 1] = arg1;
			pos += 2;
		}
		else
		{
			WithResize(ref stack, ref pos, arg0, arg1);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		static void WithResize(ref int[] reference, ref int reference2, int arg2, int arg3)
		{
			Array.Resize(ref reference, (reference2 + 1) * 2);
			StackPush(ref reference, ref reference2, arg2, arg3);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void StackPush(ref int[] stack, ref int pos, int arg0, int arg1, int arg2)
	{
		int[] array = stack;
		int num = pos;
		if ((uint)(num + 2) < (uint)array.Length)
		{
			array[num] = arg0;
			array[num + 1] = arg1;
			array[num + 2] = arg2;
			pos += 3;
		}
		else
		{
			WithResize(ref stack, ref pos, arg0, arg1, arg2);
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		static void WithResize(ref int[] reference, ref int reference2, int arg3, int arg4, int arg5)
		{
			Array.Resize(ref reference, (reference2 + 2) * 2);
			StackPush(ref reference, ref reference2, arg3, arg4, arg5);
		}
	}

	static _003CRegexGenerator_g_003EFEAF1E475058F2A5076BF7A12FD7EC10EB55C316C09845D096938DB852003224B__Utilities()
	{
		InlineArray2<string> buffer = default(InlineArray2<string>);
		buffer[0] = "http";
		buffer[1] = "soc";
		s_indexOfAnyStrings_OrdinalIgnoreCase_5A4D9B017D3E6477E32C9603AB4180D9672961E82A4AB9ED0C76157EDE808508 = SearchValues.Create(buffer, StringComparison.OrdinalIgnoreCase);
	}
}
