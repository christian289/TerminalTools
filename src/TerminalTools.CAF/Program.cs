using TerminalTools.CAF.Commands;
using TerminalTools.CAF.Filters;
using TerminalTools.Core;

var app = ConsoleApp.Create()
    .ConfigureDefaultConfiguration(config =>
    {
        if (OperatingSystem.IsWindows())
        {
            config.SetBasePath(AppContext.BaseDirectory);
            config.AddJsonFile("appsettings.windows.json", optional: true, reloadOnChange: true);
        }
        else
        {
            config.SetBasePath(Directory.GetCurrentDirectory());
            config.AddJsonFile("appsettings.linux.json", optional: true, reloadOnChange: true);
        }
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddZLoggerConsole();
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .ConfigureServices((config, services) =>
    {
        services.AddSingleton<NetworkScannerService>();
    });
app.UseFilter<ReplaceZLoggerFilter>(); // ConsoleAppBuilder.UseFilter<T>는 모든 Command에 대해 전역 필터 지원
app.UseFilter<RxCancellationDetectionFilter>();
app.Add<NetworkCommand>();
await app.RunAsync(args); // RunAsync에서 약식으로 Command를 실행할 경우 parameter의 alias를 사용할 수 없으므로 Command 클래스를 정의한다.