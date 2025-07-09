namespace TerminalTools.CAF.Filters;

/// <summary>
/// A filter that replaces the logging behavior of the <see cref="ConsoleApp"/> with custom logging methods using a
/// provided <see cref="ILogger{TCategoryName}"/>.
/// </summary>
/// <remarks>This filter modifies the <see cref="ConsoleApp.Log"/> and <see cref="ConsoleApp.LogError"/> delegates
/// to use the specified logger for logging informational and error messages, respectively. It ensures that all
/// subsequent operations in the pipeline use the updated logging behavior.</remarks>
/// <param name="next">The next filter in the pipeline to invoke after this filter.</param>
/// <param name="logger">The logger used to log messages and errors.</param>
internal sealed class ReplaceZLoggerFilter(ConsoleAppFilter next, ILogger<Program> logger) : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        ConsoleApp.Log = msg => logger.ZLogInformation($"{msg}");
        ConsoleApp.LogError = msg => logger.ZLogError($"{msg}");

        return Next.InvokeAsync(context, cancellationToken);
    }
}