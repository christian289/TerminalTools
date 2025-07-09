using TerminalTools.CAF.Filters;
using TerminalTools.Core;
using TerminalTools.Core.Records;

namespace TerminalTools.CAF.Commands;

public sealed class NetworkCommand(
    ILogger<NetworkCommand> logger,
    NetworkScannerService networkService)
{
    /// <summary>
    /// Check Available Ports
    /// </summary>
    /// <param name="host">-h, Host to scan</param>
    /// <param name="portRange">-p, Port range (e.g., 1-100 or 80, 443, 8080)</param>
    /// <param name="timeout">-t, Timeout in milliseconds</param>
    /// <param name="concurrency">-c, Max concurrency</param>
    /// <returns></returns>
    //[ConsoleAppFilter<RxCancellationDetectionFilter>] // 이렇게하면 scan ommand만 CancellationDetectionFilter를 적용받으며, 사전에 전역으로 등록되어 있었다면 이 명령에 한하여 2중으로 필터가 적용된다.
    [Command("scan")]
    public async Task ScanPortsAsync(
        CancellationToken cancellationToken,
        string host = "localhost",
        string? portRange = null,
        int timeout = 3000,
        int concurrency = 50)
    {
        // Ctrl+C 감지 이벤트 구독
        var cancellationSubscription = RxCancellationDetectionFilter.CancellationRequested
            .Subscribe(timestamp =>
            {
                logger.ZLogInformation($"[{timestamp:HH:mm:ss}] 사용자가 스캔을 중단했습니다.");
                logger.ZLogInformation($"정리 작업을 수행 중...");
            });

        logger.ZLogInformation($""""=== 네트워크 포트 스캐너 v1.0 ==="""");
        logger.ZLogInformation($"대상: {host}");
        logger.ZLogInformation($"타임아웃: {timeout}ms, 동시성: {concurrency}");
        logger.ZLogInformation($"Ctrl+C를 눌러서 중단할 수 있습니다.");

        try
        {
            var ports = ParsePortRange(portRange);
            var ipAddress = await networkService.ResolveHostAsync(host, cancellationToken);

            logger.ZLogDebug($"해석된 IP: {ipAddress}");
            logger.ZLogDebug($"스캔할 포트 수: {ports.Count}\n");

            var openPorts = new List<ScanResult>();
            var totalScanned = 0;
            var semaphore = new SemaphoreSlim(concurrency);

            var tasks = ports.Select(async port =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var result = await networkService.ScanPortAsync(ipAddress, port, timeout, cancellationToken);

                    Interlocked.Increment(ref totalScanned);

                    if (result.IsOpen)
                    {
                        lock (openPorts)
                        {
                            openPorts.Add(result);
                        }

                        var serviceName = NetworkScannerService.GetServiceName(result.Port);
                        logger.ZLogInformation($"✓ 포트 {result.Port:D5}: {serviceName} 열림 ({result.ResponseTime.TotalMilliseconds:F1}ms)");
                    }

                    // 진행률 표시 (10개마다)
                    if (totalScanned % 10 == 0)
                    {
                        var progress = (double)totalScanned / ports.Count * 100;
                        logger.ZLogInformation($"\r진행률: {progress:F1}% ({totalScanned}/{ports.Count})");
                    }

                    return result;
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            // 결과 요약
            logger.ZLogInformation($"\n\n=== 스캔 결과 요약 ===");
            logger.ZLogInformation($"총 스캔 포트: {ports.Count}");
            logger.ZLogInformation($"열린 포트: {openPorts.Count}");
            logger.ZLogInformation($"닫힌 포트: {ports.Count - openPorts.Count}");

            if (openPorts.Count != 0)
            {
                logger.ZLogInformation($"=== 열린 포트 목록 ===");
                foreach (var result in openPorts.OrderBy(r => r.Port))
                {
                    var serviceName = NetworkScannerService.GetServiceName(result.Port);
                    logger.ZLogInformation($"포트 {result.Port:D5}: {serviceName} (응답시간: {result.ResponseTime.TotalMilliseconds:F1}ms)");
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.ZLogWarning($"스캔이 사용자에 의해 중단되었습니다.");
        }
        finally
        {
            cancellationSubscription.Dispose();
        }
    }

    /// <summary>
    /// Check Host Ping
    /// </summary>
    /// <param name="host">-h, Host to ping</param>
    /// <param name="timeout">-t, Count of pings</param>
    /// <param name="count">-c, Timeout in milliseconds</param>
    /// <returns></returns>
    [Command("ping")]
    public async Task PingHostAsync(
        CancellationToken cancellationToken,
        string host,
        int count = 4,
        int timeout = 5000)
    {
        logger.ZLogInformation($"=== {host} 핑 테스트 ===");
        logger.ZLogInformation($"패킷 수: {count}, 타임아웃: {timeout}ms\n");

        using var ping = new Ping();
        var successCount = 0;
        var totalTime = TimeSpan.Zero;

        for (int i = 0; i < count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var reply = await ping.SendPingAsync(host, timeout);

                if (reply.Status == IPStatus.Success)
                {
                    successCount++;
                    totalTime += TimeSpan.FromMilliseconds(reply.RoundtripTime);
                    logger.ZLogInformation($"Reply from {reply.Address}: bytes=32 time={reply.RoundtripTime}ms TTL={reply.Options?.Ttl}");
                }
                else
                {
                    logger.ZLogInformation($"Request timed out. ({reply.Status})");
                }
            }
            catch (Exception ex)
            {
                logger.ZLogError(ex, $"Ping failed: {ex.Message}");
            }

            if (i < count - 1)
                await Task.Delay(1000, cancellationToken);
        }

        logger.ZLogInformation($"=== 핑 통계 ===");
        logger.ZLogInformation($"패킷: 전송 = {count}, 수신 = {successCount}, 손실 = {count - successCount} ({(count - successCount) * 100.0 / count:F1}% loss)");

        if (successCount > 0)
        {
            var avgTime = totalTime.TotalMilliseconds / successCount;
            logger.ZLogInformation($"평균 왕복 시간: {avgTime:F1}ms");
        }
    }

    private static IReadOnlyList<int> ParsePortRange(string? portRange)
    {
        if (string.IsNullOrWhiteSpace(portRange))
            return NetworkScannerService.CommonPorts;

        var ports = new List<int>();
        var parts = portRange.Split(',', StringSplitOptions.RemoveEmptyEntries);

        foreach (var part in parts)
        {
            if (part.Contains('-'))
            {
                var range = part.Split('-');
                if (range.Length == 2 &&
                    int.TryParse(range[0], out var start) &&
                    int.TryParse(range[1], out var end))
                {
                    ports.AddRange(Enumerable.Range(start, end - start + 1));
                }
            }
            else if (int.TryParse(part, out var port))
            {
                ports.Add(port);
            }
        }

        return ports.Distinct().Where(p => p > 0 && p <= 65535).ToList().AsReadOnly();
    }
}
