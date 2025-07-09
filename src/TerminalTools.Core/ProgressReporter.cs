using TerminalTools.Core.Records;

namespace TerminalTools.Core;

// 진행률 리포터 (함수형 접근)
public static class ProgressReporter
{
    public static IObservable<ScanProgress> CreateProgressStream<T>(
        IObservable<T> source,
        int totalCount)
    {
        var startTime = DateTime.UtcNow;

        return source
            .Scan(0, (count, _) => count + 1)
            .Select(completedCount =>
            {
                var elapsed = DateTime.UtcNow - startTime;
                var percentage = (double)completedCount / totalCount * 100;

                return new ScanProgress(
                    completedCount,
                    totalCount,
                    percentage,
                    elapsed
                );
            });
    }
}