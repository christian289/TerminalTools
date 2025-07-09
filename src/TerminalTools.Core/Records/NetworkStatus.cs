namespace TerminalTools.Core.Records;

// 네트워크 상태를 나타내는 불변 레코드
public readonly record struct NetworkStatus(
    string InterfaceName,
    IPAddress IpAddress,
    bool IsUp,
    long BytesSent,
    long BytesReceived,
    DateTime Timestamp
);
