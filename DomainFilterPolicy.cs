using System;
using System.Collections.Generic;

namespace VpnHood.Core.Client.Abstractions;

public class DomainFilterPolicy
{
	public IReadOnlyList<string> Blocks { get; set; } = Array.Empty<string>();

	public IReadOnlyList<string> Excludes { get; set; } = Array.Empty<string>();

	public IReadOnlyList<string> Includes { get; set; } = Array.Empty<string>();
}
