using System.Drawing;
using Terminal.Gui;
using Mullai.TUI.TUI.State;
using Terminal.Gui.Views;
using Mullai.Abstractions.Observability;
using Terminal.Gui.Input;

namespace Mullai.TUI.TUI.Views;

/// <summary>
/// A version of ChatHistoryView that uses Terminal.Gui.TextView for better text handling.
/// </summary>
public class ChatHistoryView : TextView
{
    private const int RightMargin = 2;
    private const int LeftMargin = 1;

    public ChatHistoryView()
    {
        ReadOnly = true;
        WordWrap = true;
        CanFocus = true; // TextView usually needs focus to allow scrolling/selection
        // MouseBindings.Add (MouseFlags.RightButtonClicked, Command.Copy);
    }

    /// <summary>Recompute the full text from the current state snapshot.</summary>
    public void UpdateMessages(
        IEnumerable<object> entries,
        string streamingBuffer,
        bool isThinking)
    {
        var sb = new System.Text.StringBuilder();

        foreach (var entry in entries)
        {
            if (entry is ChatMessage msg)
            {
                var sender = msg.IsUser ? "You" : "Mullai";
                sb.AppendLine($" {sender}");
                sb.AppendLine($" {msg.Content}");
                sb.AppendLine();
            }
            else if (entry is ToolCallObservation tool)
            {
                sb.AppendLine($" [Tool: {tool.ToolName}]");
                if (tool.Succeeded)
                {
                    sb.AppendLine($" Result: {tool.Result}");
                }
                else
                {
                    sb.AppendLine($" Error: {tool.Error}");
                }
                sb.AppendLine();
            }
        }

        if (isThinking)
        {
            sb.AppendLine(" Mullai");
            var text = string.IsNullOrEmpty(streamingBuffer) ? " ● Thinking…" : $" {streamingBuffer}";
            sb.AppendLine(text);
        }

        Text = sb.ToString();

        // Scroll to the end
        // if (Lines > 0)
        // {
        //     CursorPosition = new Point(0, Lines - 1);
        //     ScrollToLine(Lines - 1);
        // }
    }
    
    // protected override bool OnMouseEvent (Mouse mouse)
    // {
    //     if (mouse.Flags.HasFlag (MouseFlags.RightButtonClicked))
    //     {
    //         // if (IsForegroundPoint (mouse.Position!.Value.X, mouse.Position!.Value.Y))
    //         // {
    //         //     ClickedInForeground ();
    //         // }
    //         // else if (IsBackgroundPoint (mouse.Position!.Value.X, mouse.Position!.Value.Y))
    //         // {
    //         //     ClickedInBackground ();
    //         // }
    //     }
    //
    //     // mouse.Handled = true;
    //
    //     return mouse.Handled;
    // }
}
