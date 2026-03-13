using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Input;
using Terminal.Gui.Drawing;
using Mullai.TUI.TUI.State;

namespace Mullai.TUI.TUI.Views;

/// <summary>
/// Left-pane view hosting the chat history bubble renderer and the input field.
/// </summary>
public class ChatView : View
{
    private readonly ChatState _state;
    private readonly ChatHistoryView _historyView;
    private readonly TextField _inputField;
    private readonly Label _inputLabel;

    public event Action<string>? OnMessageSubmitted;

    public ChatView(ChatState state)
    {
        _state = state;

        Title = "Chat";
        BorderStyle = LineStyle.Single;
        CanFocus = true;
        TabStop = TabBehavior.TabGroup;

        _historyView = new ChatHistoryView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 3,
        };

        _inputLabel = new Label
        {
            Text = "You: ",
            X = 0,
            Y = Pos.Bottom(_historyView) + 1,
            CanFocus = false,
        };

        _inputField = new TextField
        {
            X = Pos.Right(_inputLabel),
            Y = Pos.Bottom(_historyView) + 1,
            Width = Dim.Fill(),
            CanFocus = true,
            TabStop = TabBehavior.TabStop,
        };

        _inputField.KeyDown += OnInputKeyDown;

        Add(_historyView, _inputLabel, _inputField);

        _state.StateChanged += OnStateChanged;
    }

    private void OnInputKeyDown(object? sender, Key key)
    {
        if (key == Key.Enter)
        {
            var text = _inputField.Text?.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                _inputField.Text = string.Empty;
                OnMessageSubmitted?.Invoke(text);
            }
        }
    }

    private void OnStateChanged()
    {
        _historyView.UpdateMessages(_state.Messages, _state.StreamingBuffer, _state.IsThinking);

        _inputLabel.Text = _state.IsThinking ? "…    " : "You: ";
        _inputField.Enabled = !_state.IsThinking;
    }

    public void FocusInput() => _inputField.SetFocus();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _state.StateChanged -= OnStateChanged;

        base.Dispose(disposing);
    }
}
