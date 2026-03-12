using Microsoft.Extensions.Logging;

namespace Mullai.Logging.TuiLogging;

/// <summary>
/// Logger implementation that writes to the TUI log buffer instead of console.
/// </summary>
public class TuiLogger : ILogger
{
    private readonly string _categoryName;
    private readonly TuiLogBuffer _logBuffer;
    private readonly TuiLogLevel _minLogLevel;

    public TuiLogger(string categoryName, TuiLogBuffer logBuffer, TuiLogLevel minLogLevel)
    {
        _categoryName = categoryName ?? "Default";
        _logBuffer = logBuffer ?? throw new ArgumentNullException(nameof(logBuffer));
        _minLogLevel = minLogLevel;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
    {
        var tuiLevel = ConvertLevel(logLevel);
        return tuiLevel >= _minLogLevel;
    }

    public void Log<TState>(
        Microsoft.Extensions.Logging.LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        if (exception != null)
        {
            message += Environment.NewLine + exception;
        }

        var tuiLevel = ConvertLevel(logLevel);
        _logBuffer.AddLog(_categoryName, message, tuiLevel);
    }

    private static TuiLogLevel ConvertLevel(Microsoft.Extensions.Logging.LogLevel level) =>
        level switch
        {
            Microsoft.Extensions.Logging.LogLevel.Trace => TuiLogLevel.Trace,
            Microsoft.Extensions.Logging.LogLevel.Debug => TuiLogLevel.Debug,
            Microsoft.Extensions.Logging.LogLevel.Information => TuiLogLevel.Information,
            Microsoft.Extensions.Logging.LogLevel.Warning => TuiLogLevel.Warning,
            Microsoft.Extensions.Logging.LogLevel.Error => TuiLogLevel.Error,
            Microsoft.Extensions.Logging.LogLevel.Critical => TuiLogLevel.Critical,
            _ => TuiLogLevel.None,
        };
}
