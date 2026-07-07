using System;
using System.Collections.Generic;

namespace VpnHood.Core.Toolkit.Logging;

public class LogScope
{
	public List<Tuple<string, object?>> Data { get; } = new List<Tuple<string, object>>();
}
