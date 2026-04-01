using Mullai.Abstractions.Clients;
using Mullai.Abstractions.Models;
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

    public string ModelName => _mullaiClient.ModelName;
    public string ProviderName => _mullaiClient.ProviderName;

    public void RefreshClients()
    {
        _mullaiClient.RefreshClients();
    }

    public async Task InitialiseAsync()
    {
        if (_isInitialised) return;

        await _mullaiClient.InitialiseAsync();
        _isInitialised = true;

        _ = PumpToolCallsAsync();
    }

    private async Task PumpToolCallsAsync()
    {
        await foreach (var observation in ToolCallChannel.Instance.Reader.ReadAllAsync())
            _state.AddToolCall(observation);
    }

    public async Task HandleMessageAsync(string userInput)
    {
        _state.AddUserMessage(userInput);
        _state.BeginAgentResponse();

        try
        {
            var firstUpdate = true;
            await foreach (var update in _mullaiClient.RunStreamingAsync(userInput))
                if (update is string text && !string.IsNullOrEmpty(text))
                {
                    _state.AppendUpdate(text, firstUpdate);
                    if (firstUpdate) firstUpdate = false;
                }
                else if (update is MullaiUsage usage)
                {
                    // Optionally handle usage in CLI if desired, but for now just resolving the build error
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