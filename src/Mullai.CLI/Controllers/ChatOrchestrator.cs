using Mullai.Abstractions.Clients;
using Mullai.CLI.State;

namespace Mullai.CLI.Controllers;

public class ChatOrchestrator
{
    private readonly IMullaiClient _mullaiClient;
    private readonly ChatState _state;
    private bool _isInitialised;

    public ChatOrchestrator(
        IMullaiClient mullaiClient,
        ChatState state)
    {
        _mullaiClient = mullaiClient;
        _state = state;
    }

    public void RefreshClients()
    {
        _mullaiClient.RefreshClients();
    }

    public string ModelName => _mullaiClient.ModelName;
    public string ProviderName => _mullaiClient.ProviderName;

    public async Task InitialiseAsync()
    {
        if (_isInitialised)
        {
            return;
        }

        await _mullaiClient.InitialiseAsync();
        _isInitialised = true;

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
        _state.AddUserMessage(userInput);
        _state.BeginAgentResponse();

        try
        {
            var firstUpdate = true;
            await foreach (var update in _mullaiClient.RunStreamingAsync(userInput))
            {
                if (!string.IsNullOrEmpty(update))
                {
                    _state.AppendUpdate(update, firstUpdate);
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
