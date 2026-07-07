namespace Ciphra.VPN.WinUI.Services;

public record IpcMessage(string Kind, bool? Flag, string? Payload = null);
