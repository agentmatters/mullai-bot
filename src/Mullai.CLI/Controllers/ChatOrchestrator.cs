using Microsoft.Agents.AI;
using Mullai.Agents;
using Mullai.Abstractions.Configuration;
using Mullai.CLI.State;
using System.Net.Http;

namespace Mullai.CLI.Controllers;

public class ChatOrchestrator
{
    private readonly AgentFactory _agentFactory;
    private readonly ChatState _state;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
    private readonly IMullaiConfigurationManager _configManager;
    private readonly HttpClient _httpClient;
    private MullaiAgent? _agent;
    private AgentSession? _session;

    public ChatOrchestrator(
        AgentFactory agentFactory, 
        ChatState state, 
        Microsoft.Extensions.Configuration.IConfiguration configuration,
        IMullaiConfigurationManager configManager,
        HttpClient httpClient)
    {
        _agentFactory = agentFactory;
        _state = state;
        _configuration = configuration;
        _configManager = configManager;
        _httpClient = httpClient;
    }

    public void RefreshClients()
    {
        _agent?.RefreshClients(() => 
        {
            var chatClient = _agent?.ChatClient;
            if (chatClient is Mullai.Providers.MullaiChatClient mullaiClient)
            {
                var config = _configManager.GetProvidersConfig();
                var customProviders = _configManager.GetCustomProviders();
                var newClients = Mullai.Providers.MullaiChatClientFactory.BuildOrderedClients(
                    config, 
                    customProviders,
                    _configuration, 
                    _configManager, 
                    _httpClient);
                
                mullaiClient.UpdateClients(newClients);
            }
        });
    }

    public string ModelName => _agent?.ModelName ?? "Unknown";
    public string ProviderName => _agent?.ProviderName ?? "Unknown";

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
