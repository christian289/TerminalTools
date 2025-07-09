using TerminalTools.Core.Records;

namespace TerminalTools.Core;

// 네트워크 스캐너 서비스
public sealed class NetworkScannerService
{
    // 일반적인 포트 목록
    public static readonly IReadOnlyList<int> CommonPorts = new[]
    {
            21, 22, 23, 25, 53, 80, 110, 143, 443, 993, 995,
            135, 139, 445, 1433, 3389, 5432, 3306, 1521, 8080
        }.ToList().AsReadOnly();

    public async Task<ScanResult> ScanPortAsync(
        IPAddress ipAddress,
        int port,
        int timeoutMs = 3000,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            using var tcpClient = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeoutMs);

            await tcpClient.ConnectAsync(ipAddress, port).WaitAsync(cts.Token);

            var responseTime = DateTime.UtcNow - startTime;
            return new ScanResult(ipAddress, port, true, responseTime, DateTime.UtcNow);
        }
        catch (Exception)
        {
            var responseTime = DateTime.UtcNow - startTime;
            return new ScanResult(ipAddress, port, false, responseTime, DateTime.UtcNow);
        }
    }

    public async Task<IPAddress> ResolveHostAsync(string host, CancellationToken cancellationToken)
    {
        if (IPAddress.TryParse(host, out var ipAddress))
            return ipAddress;

        var hostEntry = await Dns.GetHostEntryAsync(host, cancellationToken);
        return hostEntry.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)
               ?? throw new InvalidOperationException($"호스트 '{host}'의 IPv4 주소를 찾을 수 없습니다.");
    }

    public static string GetServiceName(int port) => port switch
    {
        21 => "FTP",
        22 => "SSH",
        23 => "Telnet",
        25 => "SMTP",
        53 => "DNS",
        80 => "HTTP",
        110 => "POP3",
        143 => "IMAP",
        443 => "HTTPS",
        993 => "IMAPS",
        995 => "POP3S",
        135 => "RPC",
        139 => "NetBIOS",
        445 => "SMB",
        1433 => "SQL Server",
        3389 => "RDP",
        5432 => "PostgreSQL",
        3306 => "MySQL",
        1521 => "Oracle",
        8080 => "HTTP Proxy",
        _ => "Unknown"
    };
}
