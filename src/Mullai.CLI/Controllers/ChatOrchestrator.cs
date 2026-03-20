using Mullai.Abstractions.Clients;
using Mullai.Abstractions.Configuration;
using Mullai.Abstractions.Orchestration;
using Mullai.CLI.State;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

namespace Mullai.CLI.Controllers;

public class ChatOrchestrator
{
    private readonly IMullaiClient _mullaiClient;
    private readonly ChatState _state;
    private readonly IConfiguration _configuration;
    private readonly IMullaiConfigurationManager _configManager;
    private readonly HttpClient _httpClient;
    private readonly IChatClient _chatClient;

    public ExecutionMode CurrentMode { get; set; } = ExecutionMode.Team;

    public ChatOrchestrator(
        IMullaiClient mullaiClient,
        ChatState state, 
        IConfiguration configuration,
        IMullaiConfigurationManager configManager,
        HttpClient httpClient,
        IChatClient chatClient)
    {
        _mullaiClient = mullaiClient;
        _state = state;
        _configuration = configuration;
        _configManager = configManager;
        _httpClient = httpClient;
        _chatClient = chatClient;
    }

    public void RefreshClients()
    {
        if (_chatClient is Mullai.Providers.MullaiChatClient mullaiClient)
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
    }

    public string ModelName => (_chatClient as Mullai.Abstractions.Configuration.IMullaiChatClient)?.ActiveLabel?.Split('/').ElementAtOrDefault(1) ?? "Orchestrator";
    public string ProviderName => (_chatClient as Mullai.Abstractions.Configuration.IMullaiChatClient)?.ActiveLabel?.Split('/')[0] ?? "Mullai";

    public async Task InitialiseAsync()
    {
        await _mullaiClient.InitialiseAsync("default");
        _ = PumpUpdatesAsync();
    }

    private async Task PumpUpdatesAsync()
    {
        await foreach (var update in _mullaiClient.GetUpdatesAsync())
        {
            switch (update.Type)
            {
                case UpdateType.Token:
                    _state.AppendUpdate(update.TaskId ?? "main", update.Text ?? "", update.AgentName ?? "Mullai");
                    break;
                case UpdateType.ToolCall:
                    if (update.ToolCall != null) _state.AddToolCall(update.ToolCall);
                    break;
                case UpdateType.Status:
                    if (update.Status == "Failed") _state.AddErrorMessage(update.Text ?? "Unknown error");
                    else if (update.TaskId != null && update.Status != null)
                        _state.UpdateTaskStatus(update.TaskId, update.Status);
                    // Status updates are generally used for high-level UI progress indicators
                    break;
                case UpdateType.Graph:
                    if (update.Graph != null)
                        _state.SetGraph(update.Graph);
                    break;
                case UpdateType.Error:
                    _state.AddErrorMessage(update.Text ?? "System error");
                    break;
            }
        }
    }

    public async Task HandleMessageAsync(string userInput)
    {
        _state.AddUserMessage(userInput);
        _state.BeginAgentResponse();

        try
        {
            await _mullaiClient.SendPromptAsync(userInput, CurrentMode);
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
