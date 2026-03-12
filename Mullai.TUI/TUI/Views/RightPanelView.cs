using System.Text;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Mullai.Abstractions.Observability;
using Mullai.TUI.TUI.State;
using Mullai.Logging.TuiLogging;

namespace Mullai.TUI.TUI.Views;

/// <summary>
/// Right-side panel displaying live tool call activity and application logs.
/// Uses TextView for native selection and copying support.
/// </summary>
public class RightPanelView : View
{
    private readonly List<ToolCallObservation> _toolCalls = [];
    private readonly List<TuiLogBuffer.LogEntry> _logs = [];
    private readonly TextView _textView;
    private readonly ChatState _state;
    private readonly TuiLogBuffer _logBuffer;
    private string _currentTab = "Logs"; // "Logs" or "Tools"
    private readonly StringBuilder _textContent = new();
    private const int MaxLines = 1000;

    public RightPanelView(ChatState state)
    {
        _state = state;
        _logBuffer = TuiLoggerProvider.GetLogBuffer();

        Title = "Logs | Tool Calls (arrow keys, Shift+arrows to select, Ctrl+C to copy)";
        BorderStyle = LineStyle.Single;
        CanFocus = true;

        _textView = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = true,
        };

        Add(_textView);

        _state.StateChanged += OnStateChanged;
        _logBuffer.LogsChanged += OnLogsChanged;
    }

    private void OnStateChanged()
    {
        if (_currentTab != "Tools") return;

        // Sync only newly added entries
        var current = _state.ToolCalls;
        if (current.Count == _toolCalls.Count) return;

        for (int i = _toolCalls.Count; i < current.Count; i++)
        {
            var obs = current[i];
            _toolCalls.Add(obs);

            // Status icon
            string icon = obs.Succeeded ? "✓" : "✗";
            string elapsed = $"{obs.Elapsed.TotalSeconds:F1}s";

            // Top line: icon + tool name + elapsed
            AppendLine($" {icon} {obs.ToolName}  ({elapsed})");

            // Argument lines (compact, max 2 shown)
            var argLines = obs.Arguments
                .Take(2)
                .Select(kvp =>
                {
                    string val = kvp.Value?.ToString() ?? "null";
                    if (val.Length > 22) val = val[..22] + "…";
                    return $"   · {kvp.Key}: {val}";
                });

            foreach (var line in argLines)
                AppendLine(line);

            // If more args than 2
            if (obs.Arguments.Count > 2)
                AppendLine($"   + {obs.Arguments.Count - 2} more arg(s)");

            // Error summary if failed
            if (!obs.Succeeded && obs.Error is { } err)
            {
                string errTrunc = err.Length > 28 ? err[..28] + "…" : err;
                AppendLine($"   ⚠ {errTrunc}");
            }

            // Blank separator between tool calls
            AppendLine(string.Empty);
        }

        UpdateTextView();
    }

    private void OnLogsChanged()
    {
        if (_currentTab != "Logs") return;

        var allLogs = _logBuffer.GetLogs();
        if (allLogs.Count == _logs.Count) return;

        // Add new logs
        for (int i = _logs.Count; i < allLogs.Count; i++)
        {
            var log = allLogs[i];
            _logs.Add(log);

            // Log level icon
            string levelIcon = log.Level switch
            {
                TuiLogLevel.Error => "❌",
                TuiLogLevel.Warning => "⚠️",
                TuiLogLevel.Information => "ℹ️",
                _ => "·",
            };

            string timeStr = log.Timestamp.ToString("HH:mm:ss");
            string category = log.Category.Length > 12 ? log.Category[..12] : log.Category;

            // First line: time + level + category
            AppendLine($" {timeStr} {levelIcon} {category}");

            // Message lines - show full content without truncation
            string message = log.Message;
            var lines = message.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            foreach (var line in lines)
                AppendLine($"   {line}");

            // Blank separator
            AppendLine(string.Empty);
        }

        UpdateTextView();
    }

    private void AppendLine(string line)
    {
        // Check if we need to trim old content
        int currentLineCount = _textContent.Length > 0 
            ? _textContent.ToString().Split(Environment.NewLine).Length 
            : 0;

        if (currentLineCount >= MaxLines)
        {
            // Remove oldest lines
            var lines = _textContent.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            _textContent.Clear();
            int startIndex = Math.Min(1, lines.Length - MaxLines + 1);
            for (int i = startIndex; i < lines.Length; i++)
            {
                _textContent.AppendLine(lines[i]);
            }
        }

        _textContent.AppendLine(line);
    }

    private void UpdateTextView()
    {
        _textView.Text = _textContent.ToString();
        SetNeedsDraw();
    }

    /// <summary>Seed static info lines (e.g. agent name) shown before any tool calls arrive.</summary>
    public void SetStaticInfo(IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            AppendLine(line);
        }

        UpdateTextView();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _state.StateChanged -= OnStateChanged;
            _logBuffer.LogsChanged -= OnLogsChanged;
        }

        base.Dispose(disposing);
    }
}
