using Mullai.Abstractions.Configuration;
using Mullai.Abstractions.Models;
using Mullai.Providers;

namespace Mullai.CLI.Controllers;

public class ConfigController
{
    private readonly IMullaiConfigurationManager _configManager;
    private readonly HttpClient _httpClient;
    private MullaiProvidersConfig? _config;

    public ConfigController(IMullaiConfigurationManager configManager, HttpClient httpClient)
    {
        _configManager = configManager;
        _httpClient = httpClient;
    }

    public List<MullaiProviderDescriptor> LoadProviders()
    {
        if (_config == null) _config = _configManager.GetProvidersConfig();
        return _config.Providers;
    }

    public void SaveProviders()
    {
        if (_config != null) _configManager.SaveProvidersConfig(_config);
    }

    public bool IsProviderEnabled(string providerName, bool defaultValue)
    {
        return _configManager.IsProviderEnabled(providerName, defaultValue);
    }

    public void SetProviderEnabled(string providerName, bool enabled)
    {
        _configManager.SetProviderEnabled(providerName, enabled);
    }

    public bool IsModelEnabled(string providerName, string modelId, bool defaultValue)
    {
        return _configManager.IsModelEnabled(providerName, modelId, defaultValue);
    }

    public void SetModelEnabled(string providerName, string modelId, bool enabled)
    {
        _configManager.SetModelEnabled(providerName, modelId, enabled);
    }

    public string? GetApiKey(string providerName)
    {
        return _configManager.GetApiKey(providerName);
    }

    public void SaveApiKey(string providerName, string key)
    {
        _configManager.SaveApiKey(providerName, key);
    }

    public void DeleteApiKey(string providerName)
    {
        _configManager.DeleteApiKey(providerName);
    }

    public async Task<List<MullaiModelDescriptor>> GetModelsAsync(string providerName)
    {
        var apiKey = _configManager.GetApiKey(providerName);
        return await MullaiChatClientFactory.GetModelsForProviderAsync(providerName, _httpClient, apiKey);
    }

    public List<CustomProviderDescriptor> GetCustomProviders()
    {
        return _configManager.GetCustomProviders();
    }

    public void SaveCustomProvider(CustomProviderDescriptor provider)
    {
        _configManager.AddCustomProvider(provider);
    }

    public void RemoveCustomProvider(string name)
    {
        _configManager.RemoveCustomProvider(name);
    }
}