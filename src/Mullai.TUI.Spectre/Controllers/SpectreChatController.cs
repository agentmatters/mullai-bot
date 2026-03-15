using Microsoft.Agents.AI;
using Mullai.Agents;
using Mullai.TUI.TUI.State;

namespace Mullai.TUI.Spectre.Controllers;

public class SpectreChatController
{
    private readonly AgentFactory _agentFactory;
    private readonly ChatState _state;
    private AIAgent? _agent;
    private AgentSession? _session;

    public SpectreChatController(AgentFactory agentFactory, ChatState state)
    {
        _agentFactory = agentFactory;
        _state = state;
    }

    public async Task InitialiseAsync()
    {
        _agent = _agentFactory.GetAgent("Assistant");
        _session = await _agent.CreateSessionAsync();

        _ = PumpToolCallsAsync();
    }

    private async Task PumpToolCallsAsync()
    {
        await foreach (var observation in ToolCallChannel.Instance.Reader.ReadAllAsync())
        {
            _state.AddToolCall(observation);
        }
    }

    public async Task HandleMessageAsync(string userInput)
    {
        if (_agent is null || _session is null)
            return;

        _state.AddUserMessage(userInput);
        _state.BeginAgentResponse();

        try
        {
            var firstUpdate = true;
            await foreach (var update in _agent.RunStreamingAsync(userInput, _session))
            {
                var text = update?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(text))
                {
                    _state.AppendUpdate(text, firstUpdate);
                    if (firstUpdate) firstUpdate = false;
                }
            }
        }
        catch (Exception ex)
        {
            _state.AddErrorMessage(ex.Message);
        }
        finally
        {
            _state.CompleteAgentResponse();
        }
    }
}
