using Terminal.Gui.App;
using Microsoft.Extensions.DependencyInjection;
using Mullai.Agents;
using Mullai.Middleware.Middlewares;
using Mullai.TUI.TUI.Controllers;
using Mullai.TUI.TUI.State;
using Mullai.TUI.TUI.Views;

namespace Mullai.TUI.TUI.App;

/// <summary>
/// Entry-point for the Terminal.Gui application.
/// Resolves dependencies, wires views together, and runs the event loop.
/// </summary>
public class MullaiApp
{
    private readonly IServiceProvider _services;

    public MullaiApp(IServiceProvider services) => _services = services;

    /// <summary>Builds the TUI and blocks until the user quits.</summary>
    public async Task RunAsync()
    {
        var state = new ChatState();
        var agentFactory = _services.GetRequiredService<AgentFactory>();

        // Wire the FunctionCallingMiddleware to emit tool call observations
        // into the singleton channel. The middleware is singleton-scoped in DI
        // so setting this once covers all agent invocations.
        var middleware = _services.GetRequiredService<FunctionCallingMiddleware>();
        middleware.OnToolCallObserved = obs => ToolCallChannel.Instance.Writer.TryWrite(obs);

        using var app = Application.Create().Init();

        var controller = new ChatController(agentFactory, state, app);
        await controller.InitialiseAsync();

        var window = new MainWindow(state, app);

        window.ChatView.OnMessageSubmitted += (userInput) =>
        {
            _ = Task.Run(() => controller.HandleMessageAsync(userInput, window));
        };

        // Seed the right panel with static session info
        window.RightPanel.SetStaticInfo(["Agent: Assistant", string.Empty]);

        // Defer initial focus to the first UI iteration so layout is complete
        app.AddTimeout(TimeSpan.FromMilliseconds(50), () =>
        {
            window.ChatView.FocusInput();
            return false;
        });

        app.Run(window);
        window.Dispose();
    }

    public static async Task Launch(IServiceProvider services)
    {
        var mullaiApp = new MullaiApp(services);
        await mullaiApp.RunAsync();
    }
}
