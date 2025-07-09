namespace TerminalTools.Core.Records;

// 스캔 결과를 나타내는 불변 레코드
public readonly record struct ScanResult(
    IPAddress IpAddress,
    int Port,
    bool IsOpen,
    TimeSpan ResponseTime,
    DateTime Timestamp
);
