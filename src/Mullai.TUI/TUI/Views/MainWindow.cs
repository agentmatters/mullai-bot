using Terminal.Gui.Views;
using Terminal.Gui.ViewBase;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Mullai.TUI.TUI.State;

namespace Mullai.TUI.TUI.Views;

/// <summary>
/// Root window. Owns the top-level layout:
/// <list type="bullet">
///   <item>MenuBar  — File | Help</item>
///   <item>ChatView — left pane (main content)</item>
///   <item>RightPanelView — right pane (~30 cols)</item>
///   <item>StatusBarView  — docked at the bottom</item>
/// </list>
/// </summary>
public class MainWindow : Window
{
    public ChatView ChatView { get; }
    public RightPanelView RightPanel { get; }
    public StatusBarView StatusBar { get; }

    private readonly IApplication _app;
    private const int RightPanelWidth = 100;

    public MainWindow(ChatState state, IApplication app)
    {
        _app = app;
        Title = "Mullai";
        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();

        var menuBar = BuildMenuBar();

        ChatView = new ChatView(state)
        {
            X = 0,
            Y = Pos.Bottom(menuBar),
            Width = Dim.Fill() - RightPanelWidth,
            Height = Dim.Fill() - 1,
        };

        RightPanel = new RightPanelView(state)
        {
            X = Pos.Right(ChatView),
            Y = Pos.Bottom(menuBar),
            Width = RightPanelWidth,
            Height = Dim.Fill() - 1,
        };

        StatusBar = new StatusBarView
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
        };

        Add(menuBar, ChatView, RightPanel, StatusBar);
    }

    private MenuBar BuildMenuBar()
    {
        return new MenuBar
        {
            Menus =
            [
                new MenuBarItem("_File",
                [
                    new MenuItem("_Quit", "Ctrl+Q · Exit Mullai", () => _app.RequestStop()),
                ]),
                new MenuBarItem("_Help",
                [
                    new MenuItem("_About", "", OnAbout),
                ]),
            ],
        };
    }

    private void OnAbout()
    {
        MessageBox.Query(_app, "About Mullai", "Mullai — AI Chat Console\n\nPowered by Terminal.Gui v2", "OK");
    }

    protected override bool OnKeyDown(Key keyEvent)
    {
        if (keyEvent == Key.Q.WithCtrl)
        {
            _app.RequestStop();
            return true;
        }
        return base.OnKeyDown(keyEvent);
    }
}
