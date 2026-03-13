using System.Collections.ObjectModel;
using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Drawing;
using Mullai.Abstractions.Observability;
using Mullai.TUI.TUI.State;

namespace Mullai.TUI.TUI.Views;

/// <summary>
/// Right-side panel displaying live tool call activity.
/// The panel uses a custom-rendered list with status icons and timing.
/// </summary>
public class RightPanelView : View
{
    private readonly List<ToolCallObservation> _toolCalls = [];
    private readonly ObservableCollection<string> _displayItems = [];
    private readonly ListView _listView;
    private readonly ChatState _state;

    public RightPanelView(ChatState state)
    {
        _state = state;

        Title = "Tool Calls";
        BorderStyle = LineStyle.Single;
        CanFocus = false;

        _listView = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        _listView.SetSource(_displayItems);

        Add(_listView);

        _state.StateChanged += OnStateChanged;
    }

    private void OnStateChanged()
    {
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
            _displayItems.Add($" {icon} {obs.ToolName}  ({elapsed})");

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
                _displayItems.Add(line);

            // If more args than 2
            if (obs.Arguments.Count > 2)
                _displayItems.Add($"   + {obs.Arguments.Count - 2} more arg(s)");

            // Error summary if failed
            if (!obs.Succeeded && obs.Error is { } err)
            {
                string errTrunc = err.Length > 28 ? err[..28] + "…" : err;
                _displayItems.Add($"   ⚠ {errTrunc}");
            }

            // Blank separator between tool calls
            _displayItems.Add(string.Empty);
        }

        // Scroll to latest
        _listView.SelectedItem = Math.Max(0, _displayItems.Count - 1);
        _listView.EnsureSelectedItemVisible();

        SetNeedsDraw();
    }

    /// <summary>Seed static info lines (e.g. agent name) shown before any tool calls arrive.</summary>
    public void SetStaticInfo(IEnumerable<string> lines)
    {
        foreach (var line in lines)
            _displayItems.Insert(_displayItems.Count == 0 ? 0 : _displayItems.Count, line);

        SetNeedsDraw();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _state.StateChanged -= OnStateChanged;

        base.Dispose(disposing);
    }
}
