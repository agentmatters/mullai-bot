using TGuiAttr = Terminal.Gui.Drawing.Attribute;
using TGuiColor = Terminal.Gui.Drawing.Color;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Input;
using Mullai.TUI.TUI.State;

namespace Mullai.TUI.TUI.Views;

/// <summary>
/// Custom view that renders chat messages as styled bubbles:
/// - User messages: right-aligned, deep-blue background, white text
/// - Agent messages: left-aligned, dark-green background, light-green text
/// - Streaming (in-progress): left-aligned, yellow text
/// Supports built-in v2 vertical scrolling.
/// </summary>
public class ChatHistoryView : View
{
    private readonly List<RenderLine> _lines = [];

    private record RenderLine(string Text, MessageKind Kind);

    private enum MessageKind { User, Agent, Streaming, Separator }

    // ── TrueColor palette ────────────────────────────────────────────────────
    private static readonly TGuiAttr UserAttr = new(
        new TGuiColor(0xFF, 0xFF, 0xFF),
        new TGuiColor(0x1B, 0x4F, 0x9E));

    private static readonly TGuiAttr UserLabelAttr = new(
        new TGuiColor(0xAD, 0xD8, 0xFF),
        new TGuiColor(0x1B, 0x4F, 0x9E));

    private static readonly TGuiAttr AgentAttr = new(
        new TGuiColor(0xD4, 0xFF, 0xD4),
        new TGuiColor(0x1A, 0x28, 0x1A));

    private static readonly TGuiAttr AgentLabelAttr = new(
        new TGuiColor(0x5C, 0xFF, 0x87),
        new TGuiColor(0x1A, 0x28, 0x1A));

    private static readonly TGuiAttr StreamAttr = new(
        new TGuiColor(0xFF, 0xFF, 0x88),
        new TGuiColor(0x1A, 0x28, 0x1A));

    // Background clear colour using the view's current scheme
    private TGuiAttr NormalAttr =>
        new TGuiAttr(new TGuiColor(0xCC, 0xCC, 0xCC), new TGuiColor(0x12, 0x12, 0x12));

    private const int RightMargin = 2;
    private const int LeftMargin = 1;

    public ChatHistoryView()
    {
        CanFocus = false;
        ViewportSettings |= ViewportSettingsFlags.HasVerticalScrollBar;

        // Ensure mouse events (including wheel) are dispatched to this view
        // even though it is not focusable.
        MouseBindings.Add(MouseFlags.WheeledDown, Command.ScrollDown);
        MouseBindings.Add(MouseFlags.WheeledUp, Command.ScrollUp);
    }

    /// <summary>Recompute render lines from the current state snapshot and request a redraw.</summary>
    public void UpdateMessages(
        IReadOnlyList<ChatMessage> messages,
        string streamingBuffer,
        bool isThinking)
    {
        _lines.Clear();

        int bubbleW = BubbleWidth();

        foreach (var msg in messages)
        {
            _lines.Add(new(msg.IsUser ? "You" : "Mullai",
                           msg.IsUser ? MessageKind.User : MessageKind.Agent));

            foreach (var line in WordWrap(msg.Content, bubbleW))
                _lines.Add(new(line, msg.IsUser ? MessageKind.User : MessageKind.Agent));

            _lines.Add(new(string.Empty, MessageKind.Separator));
        }

        if (isThinking)
        {
            _lines.Add(new("Mullai", MessageKind.Streaming));
            var text = string.IsNullOrEmpty(streamingBuffer) ? "● Thinking…" : streamingBuffer;
            foreach (var line in WordWrap(text, bubbleW))
                _lines.Add(new(line, MessageKind.Streaming));
        }

        // Expand content height and scroll to bottom
        int vh = Viewport.Height > 0 ? Viewport.Height : 20;
        int vw = Viewport.Width > 0 ? Viewport.Width : 80;
        SetContentSize(new System.Drawing.Size(vw, Math.Max(_lines.Count + 1, vh)));
        if (_lines.Count > 0) ScrollVertical(_lines.Count);

        SetNeedsDraw();
    }

    protected override bool OnDrawingContent(DrawContext? context)
    {
        var vp = Viewport;
        int width = vp.Width;

        for (int i = 0; i < _lines.Count; i++)
        {
            int row = i - vp.Y;
            if (row < 0) continue;
            if (row >= vp.Height) break;

            var rl = _lines[i];

            if (rl.Kind == MessageKind.Separator || string.IsNullOrEmpty(rl.Text))
            {
                SetAttribute(NormalAttr);
                Move(0, row);
                AddStr(new string(' ', width));
                continue;
            }

            switch (rl.Kind)
            {
                case MessageKind.User:
                    DrawUserLine(rl.Text, row, width);
                    break;
                case MessageKind.Agent:
                    DrawAgentLine(rl.Text, row, width, streaming: false);
                    break;
                case MessageKind.Streaming:
                    DrawAgentLine(rl.Text, row, width, streaming: true);
                    break;
            }
        }

        return true;
    }

    /// <summary>
    /// Handle mouse wheel events for scrolling when the cursor is over this view.
    /// </summary>
    protected override bool OnMouseEvent(Mouse mouse)
    {
        if (mouse.Flags.HasFlag(MouseFlags.WheeledDown))
        {
            ScrollVertical(3);
            SetNeedsDraw();
            return true;
        }

        if (mouse.Flags.HasFlag(MouseFlags.WheeledUp))
        {
            ScrollVertical(-3);
            SetNeedsDraw();
            return true;
        }

        return base.OnMouseEvent(mouse);
    }
    // ── Drawing helpers ──────────────────────────────────────────────────────

    private void DrawUserLine(string text, int row, int viewWidth)
    {
        bool isLabel = text == "You";
        var attr = isLabel ? UserLabelAttr : UserAttr;
        string content = isLabel ? $" {text} ▶ " : $"  {text}  ";
        int maxBubble = (int)(viewWidth * 0.72);
        if (content.Length > maxBubble) content = content[..maxBubble];

        int bubbleStart = Math.Max(0, viewWidth - content.Length - RightMargin);

        SetAttribute(NormalAttr);
        Move(0, row);
        AddStr(new string(' ', bubbleStart));

        SetAttribute(attr);
        AddStr(content);

        SetAttribute(NormalAttr);
        int rightFill = viewWidth - bubbleStart - content.Length;
        if (rightFill > 0) AddStr(new string(' ', rightFill));
    }

    private void DrawAgentLine(string text, int row, int viewWidth, bool streaming)
    {
        bool isLabel = text == "Mullai";
        var attr = streaming ? StreamAttr : (isLabel ? AgentLabelAttr : AgentAttr);
        string content = isLabel ? $" ◀ {text} " : $"   {text}  ";
        int maxBubble = (int)(viewWidth * 0.72);
        if (content.Length > maxBubble) content = content[..maxBubble];

        SetAttribute(NormalAttr);
        Move(0, row);
        AddStr(new string(' ', LeftMargin));

        SetAttribute(attr);
        AddStr(content);

        SetAttribute(NormalAttr);
        int filled = LeftMargin + content.Length;
        if (filled < viewWidth) AddStr(new string(' ', viewWidth - filled));
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    private int BubbleWidth()
    {
        int w = Viewport.Width > 0 ? Viewport.Width : GetContentSize().Width;
        return w > 0 ? (int)(w * 0.72) : 60;
    }

    private static IEnumerable<string> WordWrap(string text, int maxWidth)
    {
        if (maxWidth <= 4) { yield return text; yield break; }

        foreach (var paragraph in text.Split('\n'))
        {
            if (string.IsNullOrEmpty(paragraph)) { yield return string.Empty; continue; }

            var words = paragraph.Split(' ');
            var sb = new System.Text.StringBuilder();

            foreach (var word in words)
            {
                if (sb.Length + word.Length + 1 > maxWidth && sb.Length > 0)
                {
                    yield return sb.ToString();
                    sb.Clear();
                }
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(word);
            }
            if (sb.Length > 0) yield return sb.ToString();
        }
    }
}
