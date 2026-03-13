using Terminal.Gui.App;
using Microsoft.Agents.AI;
using Mullai.Agents;
using Mullai.TUI.TUI.State;
using Mullai.TUI.TUI.Views;

namespace Mullai.TUI.TUI.Controllers;

/// <summary>
/// Mediates between the user-facing <see cref="MainWindow"/> and the AI agent.
/// Also pumps <see cref="ToolCallChannel"/> events onto the UI thread so the
/// right panel updates in real time as tools are invoked.
/// </summary>
public class ChatController
{
    private readonly AgentFactory _agentFactory;
    private readonly ChatState _state;
    private readonly IApplication _app;
    private AIAgent? _agent;
    private AgentSession? _session;

    public ChatController(AgentFactory agentFactory, ChatState state, IApplication app)
    {
        _agentFactory = agentFactory;
        _state = state;
        _app = app;
    }

    /// <summary>Initialise the agent and open a new session. Call once on startup.</summary>
    public async Task InitialiseAsync()
    {
        _agent = _agentFactory.GetAgent("Assistant");
        _session = await _agent.CreateSessionAsync();

        // Start a background loop that pumps tool call observations from the
        // singleton channel onto the UI thread as they arrive.
        _ = PumpToolCallsAsync();
    }

    /// <summary>
    /// Handle a user message submitted from the UI.
    /// Updates <see cref="ChatState"/> which the views observe.
    /// </summary>
    public async Task HandleMessageAsync(string userInput, MainWindow window)
    {
        if (_agent is null || _session is null)
            return;

        _app.Invoke(() => _state.AddUserMessage(userInput));
        _app.Invoke(() =>
        {
            _state.BeginAgentResponse();
            window.StatusBar.SetStatus("Agent is thinking…");
        });

        try
        {
            await foreach (var update in _agent.RunStreamingAsync(userInput, _session))
            {
                var text = update?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(text))
                {
                    var captured = text;
                    _app.Invoke(() => _state.AppendToken(captured));
                }
            }
        }
        catch (Exception ex)
        {
            _app.Invoke(() => _state.AddErrorMessage(ex.Message));
        }
        finally
        {
            _app.Invoke(() =>
            {
                _state.CommitAgentResponse();
                window.StatusBar.SetStatus("Ready");
                window.ChatView.FocusInput();
            });
        }
    }

    /// <summary>
    /// Reads tool call observations from the singleton channel and marshals 
    /// them onto the UI thread indefinitely until the channel is closed.
    /// </summary>
    private async Task PumpToolCallsAsync()
    {
        await foreach (var observation in ToolCallChannel.Instance.Reader.ReadAllAsync())
        {
            var captured = observation;
            _app.Invoke(() => _state.AddToolCall(captured));
        }
    }
}
