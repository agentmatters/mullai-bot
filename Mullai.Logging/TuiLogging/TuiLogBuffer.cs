using System.Collections.Generic;

namespace Mullai.Logging.TuiLogging;

/// <summary>
/// Thread-safe buffer for collecting log entries to display in the TUI.
/// </summary>
public class TuiLogBuffer
{
    private readonly List<LogEntry> _entries = [];
    private readonly object _lockObj = new();
    private const int MaxEntries = 500;

    public event Action? LogsChanged;

    public record LogEntry(
        string Category,
        string Message,
        TuiLogLevel Level,
        DateTime Timestamp
    );

    public void AddLog(string category, string message, TuiLogLevel level)
    {
        lock (_lockObj)
        {
            _entries.Add(new LogEntry(category, message, level, DateTime.UtcNow));

            // Keep buffer size manageable
            if (_entries.Count > MaxEntries)
            {
                _entries.RemoveRange(0, _entries.Count - MaxEntries);
            }
        }

        LogsChanged?.Invoke();
    }

    public IReadOnlyList<LogEntry> GetLogs()
    {
        lock (_lockObj)
        {
            return _entries.AsReadOnly();
        }
    }

    public void Clear()
    {
        lock (_lockObj)
        {
            _entries.Clear();
        }
    }
}

/// <summary>Log level enumeration for TUI display.</summary>
public enum TuiLogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical,
    None
}
