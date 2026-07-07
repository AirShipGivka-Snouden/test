using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.CompilerServices;
using VpnHood.Core.Toolkit.Converters;
using VpnHood.Core.Toolkit.Net;

namespace VpnHood.Core.Toolkit;

public class AotPreserveHelper
{
	private string? _instanceId;

	[MethodImpl(MethodImplOptions.NoInlining)]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IPEndPoint))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IPAddress))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Version))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TimeSpan))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ArrayConverter<, >))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NullToEmptyArrayConverter<>))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IPAddressConverter))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IPEndPointConverter))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TimeSpanConverter))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(VersionConverter))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IpRange))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IpRangeConverter))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IPNetwork))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IpNetwork))]
	[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IpNetworkConverter))]
	public string PreserveTypes()
	{
		if (_instanceId == null)
		{
			_instanceId = Guid.NewGuid().ToString();
		}
		return _instanceId;
	}
}
