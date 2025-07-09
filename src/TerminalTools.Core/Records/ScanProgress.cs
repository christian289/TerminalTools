namespace TerminalTools.Core.Records;

// 포트 스캔 진행 상황
public readonly record struct ScanProgress(
    int CompletedScans,
    int TotalScans,
    double PercentageComplete,
    TimeSpan ElapsedTime
);
