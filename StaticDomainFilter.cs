using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace VpnHood.Core.Filtering.Abstractions;

public class StaticDomainFilter(IDomainFilter? nextFilter) : IDomainFilter, IDisposable
{
	private string[] _invertedBlocks = Array.Empty<string>();

	private string[] _invertedExcludes = Array.Empty<string>();

	private string[] _invertedIncludes = Array.Empty<string>();

	[CompilerGenerated]
	private IReadOnlyList<string> _003CBlocks_003Ek__BackingField = Array.Empty<string>();

	[CompilerGenerated]
	private IReadOnlyList<string> _003CExcludes_003Ek__BackingField = Array.Empty<string>();

	[CompilerGenerated]
	private IReadOnlyList<string> _003CIncludes_003Ek__BackingField = Array.Empty<string>();

	public bool IsEmpty
	{
		get
		{
			if (Blocks.Count == 0 && Excludes.Count == 0)
			{
				return Includes.Count == 0;
			}
			return false;
		}
	}

	public IReadOnlyList<string> Blocks
	{
		[CompilerGenerated]
		get
		{
			return _003CBlocks_003Ek__BackingField;
		}
		set
		{
			_003CBlocks_003Ek__BackingField = value;
			_invertedBlocks = BuildSortedInvertedArray(value);
		}
	}

	public IReadOnlyList<string> Excludes
	{
		[CompilerGenerated]
		get
		{
			return _003CExcludes_003Ek__BackingField;
		}
		set
		{
			_003CExcludes_003Ek__BackingField = value;
			_invertedExcludes = BuildSortedInvertedArray(value);
		}
	}

	public IReadOnlyList<string> Includes
	{
		[CompilerGenerated]
		get
		{
			return _003CIncludes_003Ek__BackingField;
		}
		set
		{
			_003CIncludes_003Ek__BackingField = value;
			_invertedIncludes = BuildSortedInvertedArray(value);
		}
	}

	private static string[] BuildSortedInvertedArray(IReadOnlyList<string> domains)
	{
		return (from d in domains.Select(NormalizeDomain)
			where d.Length > 0
			select d).Select(InvertDomain).OrderBy<string, string>((string d) => d, StringComparer.Ordinal).ToArray();
	}

	private static string NormalizeDomain(string? domain)
	{
		if (string.IsNullOrWhiteSpace(domain))
		{
			return string.Empty;
		}
		string text = domain.Trim().ToLowerInvariant();
		if (text.StartsWith("*.", StringComparison.Ordinal))
		{
			text = text.Substring(2);
		}
		return text;
	}

	private static string InvertDomain(string domain)
	{
		string[] array = domain.Split('.');
		Array.Reverse(array);
		return string.Join('.', array);
	}

	public FilterAction Process(string? domain)
	{
		string text = NormalizeDomain(domain);
		if (text.Length == 0)
		{
			return FilterAction.Default;
		}
		string invertedDomain = InvertDomain(text);
		if (IsMatch(invertedDomain, _invertedBlocks))
		{
			return FilterAction.Block;
		}
		if (IsMatch(invertedDomain, _invertedExcludes))
		{
			return FilterAction.Exclude;
		}
		if (IsMatch(invertedDomain, _invertedIncludes))
		{
			return FilterAction.Include;
		}
		return nextFilter?.Process(domain) ?? FilterAction.Default;
	}

	private static bool IsMatch(string invertedDomain, string[] sortedInvertedDomains)
	{
		if (sortedInvertedDomains.Length == 0)
		{
			return false;
		}
		int num = Array.BinarySearch(sortedInvertedDomains, invertedDomain, (IComparer<string>?)StringComparer.Ordinal);
		if (num >= 0)
		{
			return true;
		}
		int num2 = ~num;
		if (num2 > 0)
		{
			string candidate = sortedInvertedDomains[num2 - 1];
			if (IsWildcardMatch(invertedDomain, candidate))
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsWildcardMatch(string invertedDomain, string candidate)
	{
		if (invertedDomain.Length > candidate.Length && invertedDomain[candidate.Length] == '.')
		{
			return invertedDomain.AsSpan(0, candidate.Length).SequenceEqual(candidate.AsSpan());
		}
		return false;
	}

	public void Dispose()
	{
	}
}
