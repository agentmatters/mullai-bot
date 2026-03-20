using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mullai.Providers.Common;
using Mullai.Providers.LLMProviders;
using Mullai.Providers.LLMProviders.Mistral;
using Mullai.Providers.LLMProviders.OpenRouter;
using Mullai.Abstractions.Models;
using Mullai.Abstractions.Configuration;
using System.Text.Json;
using Mullai.Providers.LLMProviders.Gemini;
using OpenAI;

namespace Mullai.Providers;

/// <summary>
/// Builds a priority-ordered <see cref="MullaiChatClient"/> using the configuration manager.
/// </summary>
public static class MullaiChatClientFactory
{
    private static readonly List<IModelMetadataAdapter> _adapters = new()
    {
        new OpenRouterModelAdapter(),
        new MistralModelAdapter()
    };
    
    public static MullaiChatClient Create(
        IConfiguration configuration,
        IMullaiConfigurationManager configManager,
        HttpClient httpClient,
        ILogger<MullaiChatClient> logger)
    {
        var config = configManager.GetProvidersConfig();
        var customProviders = configManager.GetCustomProviders();
        
        var clients = BuildOrderedClients(config, customProviders, configuration, configManager, httpClient);

        return new MullaiChatClient(clients, logger);
    }

    public static List<(string Label, IChatClient Client)> BuildOrderedClients(
        MullaiProvidersConfig config,
        List<CustomProviderDescriptor> customProviders,
        IConfiguration configuration,
        IMullaiConfigurationManager configManager,
        HttpClient httpClient)
    {
        var result = new List<(string, IChatClient)>();

        // Standard Providers
        var enabledProviders = config.Providers
            .Where(p => p.Enabled && configManager.IsProviderEnabled(p.Name, true))
            .OrderBy(p => p.Priority);

        foreach (var provider in enabledProviders)
        {
            var enabledModels = provider.Models
                .Where(m => m.Enabled && configManager.IsModelEnabled(provider.Name, m.ModelId, true))
                .OrderBy(m => m.Priority);

            foreach (var model in enabledModels)
            {
                var label = $"{provider.Name}/{model.ModelId}";
                var client = TryCreateClient(provider.Name, model.ModelId, configuration, configManager, httpClient);
                if (client is not null)
                {
                    result.Add((label, client));
                }
            }
        }

        // Custom Providers
        foreach (var custom in customProviders.Where(cp => cp.Enabled))
        {
            foreach (var modelId in custom.Models)
            {
                var label = $"{custom.Name}/{modelId}";
                var client = CreateCustomClient(custom, modelId);
                if (client != null)
                {
                    result.Add((label, client));
                }
            }
        }

        return result;
    }

    private static IChatClient? TryCreateClient(
        string providerName,
        string modelId,
        IConfiguration configuration,
        IMullaiConfigurationManager configManager,
        HttpClient httpClient)
    {
        var apiKey = configManager.GetApiKey(providerName);
        var effectiveConfig = apiKey != null 
            ? OverlayApiKey(configuration, providerName, apiKey) 
            : configuration;

        try
        {
            return providerName switch
            {
                "Mistral"     => Mistral.GetMistralChatClient(effectiveConfig, httpClient, modelId),
                "OpenRouter"  => OpenRouter.GetOpenRouterChatClient(effectiveConfig, httpClient, modelId),
                "Gemini"  => Gemini.GetGeminiChatClient(effectiveConfig, httpClient, modelId),
                _ => null
            };
        }
        catch { return null; }
    }

    private static IChatClient? CreateCustomClient(CustomProviderDescriptor custom, string modelId)
    {
        try
        {
            var options = new OpenAIClientOptions { Endpoint = new Uri(custom.BaseUrl) };
            var openAIClient = new OpenAIClient(new ApiKeyCredential(custom.ApiKey ?? "no-key"), options);
            return openAIClient.GetChatClient(modelId).AsIChatClient();
        }
        catch { return null; }
    }

    private static IConfiguration OverlayApiKey(IConfiguration original, string providerName, string apiKey)
    {
        var dict = new Dictionary<string, string?> { [$"{providerName}:ApiKey"] = apiKey };
        return new ConfigurationBuilder().AddConfiguration(original).AddInMemoryCollection(dict).Build();
    }

    public static async Task<List<MullaiModelDescriptor>> GetModelsForProviderAsync(
        string providerName, 
        HttpClient httpClient, 
        string? apiKey = null)
    {
        var adapter = _adapters.FirstOrDefault(a => a.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));
        if (adapter != null)
        {
            return await adapter.FetchModelsAsync(httpClient, apiKey);
        }
        return new List<MullaiModelDescriptor>();
    }
}

