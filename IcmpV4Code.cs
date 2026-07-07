namespace VpnHood.Core.Packets;

public enum IcmpV4Code : byte
{
	EchoReply = 0,
	NetUnreachable = 0,
	HostUnreachable = 1,
	ProtocolUnreachable = 2,
	PortUnreachable = 3,
	FragmentationNeeded = 4,
	SourceRouteFailed = 5,
	DestinationNetworkUnknown = 6,
	DestinationHostUnknown = 7,
	SourceHostIsolated = 8,
	NetworkAdminProhibited = 9,
	HostAdminProhibited = 10,
	NetworkUnreachableForTos = 11,
	HostUnreachableForTos = 12,
	CommunicationAdminProhibited = 13,
	HostPrecedenceViolation = 14,
	PrecedenceCutoffInEffect = 15,
	RedirectDatagramForNetwork = 0,
	RedirectDatagramForHost = 1,
	RedirectForTosAndNetwork = 2,
	RedirectForTosAndHost = 3,
	TimeToLiveExceededInTransit = 0,
	FragmentReassemblyTimeExceeded = 1,
	PointerIndicatesError = 0,
	MissingRequiredOption = 1,
	BadLength = 2
}
