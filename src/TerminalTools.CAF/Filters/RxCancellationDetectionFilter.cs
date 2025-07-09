namespace TerminalTools.CAF.Filters;

internal class RxCancellationDetectionFilter(ConsoleAppFilter next, ILogger<Program> logger) : ConsoleAppFilter(next)
{
    private static readonly Subject<DateTime> _cancellationSubject = new();
    public static IObservable<DateTime> CancellationRequested => _cancellationSubject.AsObservable();

    public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        var startTime = Stopwatch.GetTimestamp();
        logger.ZLogInformation($"명령 시작: {context.CommandName} at {DateTime.Now:HH:mm:ss}");

        try
        {
            // CancellationToken 상태 모니터링 시작
            _ = MonitorCancellationAsync(cancellationToken);

            await Next.InvokeAsync(context, cancellationToken);

            var elapsed = Stopwatch.GetElapsedTime(startTime);
            logger.ZLogInformation($"명령 완료: {context.CommandName}, 실행시간: {elapsed:mm\\:ss\\.fff}");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            var elapsed = Stopwatch.GetElapsedTime(startTime);

            // Ctrl+C 감지 이벤트 발생
            _cancellationSubject.OnNext(DateTime.Now);

            logger.ZLogWarning($"명령 중단됨 (Ctrl+C): {context.CommandName}, 실행시간: {elapsed:mm\\:ss\\.fff}");

            // 정리 작업을 위한 잠시 대기
            await Task.Delay(100, CancellationToken.None); // CancellationToken에 영향을 받지 않도록 CancellationToken.None을 통한 의도적인 명시.
            throw;
        }
        catch (Exception ex)
        {
            var elapsed = Stopwatch.GetElapsedTime(startTime);
            logger.ZLogError($"명령 실패: {context.CommandName}, 실행시간: {elapsed:mm\\:ss\\.fff}, 오류: {ex.Message}");
            throw;
        }
    }

    private async Task MonitorCancellationAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // CancellationToken이 요청되었을 때의 추가 처리가 가능
            logger.ZLogTrace($"[취소 감지] Ctrl+C가 감지되었습니다.");
        }
    }
}
