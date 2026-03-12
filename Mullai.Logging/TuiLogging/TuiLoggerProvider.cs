using Microsoft.Extensions.Logging;

namespace Mullai.Logging.TuiLogging;

/// <summary>
/// Logger provider that creates TUI loggers connected to a shared log buffer.
/// </summary>
public class TuiLoggerProvider : ILoggerProvider
{
    private readonly TuiLogBuffer _logBuffer;
    private readonly TuiLogLevel _minLogLevel;
    private readonly Dictionary<string, TuiLogger> _loggers = [];
    private bool _disposed;

    /// <summary>
    /// Singleton instance of the log buffer shared across the application.
    /// </summary>
    public static readonly TuiLogBuffer SharedLogBuffer = new();

    public TuiLoggerProvider(TuiLogLevel minLogLevel = TuiLogLevel.Information)
    {
        _logBuffer = SharedLogBuffer;
        _minLogLevel = minLogLevel;
    }

    public ILogger CreateLogger(string categoryName)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(TuiLoggerProvider));

        lock (_loggers)
        {
            if (!_loggers.TryGetValue(categoryName, out var logger))
            {
                logger = new TuiLogger(categoryName, _logBuffer, _minLogLevel);
                _loggers[categoryName] = logger;
            }

            return logger;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        lock (_loggers)
        {
            _loggers.Clear();
        }

        _disposed = true;
    }

    /// <summary>Get the shared log buffer instance for UI consumption.</summary>
    public static TuiLogBuffer GetLogBuffer() => SharedLogBuffer;
}
