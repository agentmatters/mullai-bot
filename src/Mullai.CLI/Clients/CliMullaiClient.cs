using Microsoft.Extensions.Configuration;
using Mullai.Abstractions.Configuration;
using Mullai.Agents;
using Mullai.Agents.Clients;
using Mullai.Providers;

namespace Mullai.CLI.Clients;

public class CliMullaiClient : BaseMullaiClient
{
    private readonly IMullaiConfigurationManager _configManager;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public CliMullaiClient(
        AgentFactory agentFactory,
        IConfiguration configuration,
        IMullaiConfigurationManager configManager,
        HttpClient httpClient) : base(agentFactory)
    {
        _configuration = configuration;
        _configManager = configManager;
        _httpClient = httpClient;
    }

    public override void RefreshClients()
    {
        RefreshAgentClients(agent =>
        {
            if (agent.ChatClient is not MullaiChatClient mullaiClient) return;

            var config = _configManager.GetProvidersConfig();
            var customProviders = _configManager.GetCustomProviders();
            var newClients = MullaiChatClientFactory.BuildOrderedClients(
                config,
                customProviders,
                _configuration,
                _configManager,
                _httpClient);

            mullaiClient.UpdateClients(newClients);
        });
    }
}