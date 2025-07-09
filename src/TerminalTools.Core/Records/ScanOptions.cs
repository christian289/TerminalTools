namespace TerminalTools.Core.Records;

// 스캔 옵션을 위한 불변 레코드
public readonly record struct ScanOptions(
    string TargetHost,
    IReadOnlyList<int> Ports,
    int TimeoutMs,
    int MaxConcurrency
);
