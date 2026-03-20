using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Mullai.Abstractions.Configuration;
using Mullai.Abstractions.Models;
using Mullai.Providers;

namespace Mullai.Web.Wasm.Services;

public class WebConfigController
{
    private readonly IMullaiConfigurationManager _configManager;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IChatClient _chatClient;
    private MullaiProvidersConfig? _config;

    public WebConfigController(
        IMullaiConfigurationManager configManager,
        HttpClient httpClient,
        IConfiguration configuration,
        IChatClient chatClient)
    {
        _configManager = configManager;
        _httpClient = httpClient;
        _configuration = configuration;
        _chatClient = chatClient;
    }

    public List<MullaiProviderDescriptor> LoadProviders()
    {
        if (_config == null)
        {
            _config = _configManager.GetProvidersConfig();
        }
        return _config.Providers;
    }

    public void SaveProviders()
    {
        if (_config != null)
        {
            _configManager.SaveProvidersConfig(_config);
            RefreshClients();
        }
    }

    public bool IsProviderEnabled(string providerName, bool defaultValue) =>
        _configManager.IsProviderEnabled(providerName, defaultValue);

    public void SetProviderEnabled(string providerName, bool enabled)
    {
        _configManager.SetProviderEnabled(providerName, enabled);
        RefreshClients();
    }

    public bool IsModelEnabled(string providerName, string modelId, bool defaultValue) =>
        _configManager.IsModelEnabled(providerName, modelId, defaultValue);

    public void SetModelEnabled(string providerName, string modelId, bool enabled)
    {
        _configManager.SetModelEnabled(providerName, modelId, enabled);
        RefreshClients();
    }

    public string? GetApiKey(string providerName) =>
        _configManager.GetApiKey(providerName);

    public void SaveApiKey(string providerName, string key)
    {
        _configManager.SaveApiKey(providerName, key);
        RefreshClients();
    }

    public void DeleteApiKey(string providerName)
    {
        _configManager.DeleteApiKey(providerName);
        RefreshClients();
    }

    public async Task<List<MullaiModelDescriptor>> GetModelsAsync(string providerName)
    {
        var apiKey = _configManager.GetApiKey(providerName);
        return await MullaiChatClientFactory.GetModelsForProviderAsync(providerName, _httpClient, apiKey);
    }

    private void RefreshClients()
    {
        if (_chatClient is MullaiChatClient mullaiClient)
        {
            var config = _configManager.GetProvidersConfig();
            var customProviders = _configManager.GetCustomProviders();
            var newClients = MullaiChatClientFactory.BuildOrderedClients(
                config,
                customProviders,
                _configuration,
                _configManager,
                _httpClient);

            mullaiClient.UpdateClients(newClients);
        }
    }
}
