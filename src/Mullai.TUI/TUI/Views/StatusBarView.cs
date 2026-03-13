using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Input;

namespace Mullai.TUI.TUI.Views;

/// <summary>
/// Bottom status bar. Wraps the built-in <see cref="StatusBar"/> and exposes
/// a <see cref="SetStatus"/> helper for dynamic status messages.
/// </summary>
public class StatusBarView : StatusBar
{
    private readonly Shortcut _statusShortcut;

    public StatusBarView()
    {
        _statusShortcut = new Shortcut
        {
            Title = "Ready",
            CanFocus = false,
        };

        Add(
            new Shortcut
            {
                Key = Key.Q.WithCtrl,
                Title = "Quit",
                CanFocus = false,
            },
            new Shortcut
            {
                Key = Key.F1,
                Title = "Help",
                CanFocus = false,
            },
            _statusShortcut
        );
    }

    /// <summary>Update the status message displayed in the bar.</summary>
    public void SetStatus(string status)
    {
        _statusShortcut.Title = status;
        SetNeedsDraw();
    }
}
