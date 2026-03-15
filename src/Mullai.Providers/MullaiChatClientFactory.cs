using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mullai.Providers.LLMProviders.Cerebras;
using Mullai.Providers.LLMProviders.Gemini;
using Mullai.Providers.LLMProviders.Groq;
using Mullai.Providers.LLMProviders.Mistral;
using Mullai.Providers.LLMProviders.OllamaOpenAI;
using Mullai.Providers.LLMProviders.OpenRouter;
using Mullai.Providers.Models;
using Mullai.Abstractions.Configuration;
using System.Text.Json;

namespace Mullai.Providers;

/// <summary>
/// Reads models.json, cross-references API keys from appsettings or secure storage,
/// and builds a priority-ordered <see cref="MullaiChatClient"/>.
/// </summary>
public static class MullaiChatClientFactory
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>
    /// Creates a <see cref="MullaiChatClient"/> from the given models.json path and configuration.
    /// </summary>
    /// <param name="modelsJsonPath">Absolute path to models.json.</param>
    /// <param name="configuration">appsettings configuration (for API keys).</param>
    /// <param name="credentialStorage">Secure storage for API keys.</param>
    /// <param name="httpClient">Shared HttpClient for OpenAI-compatible providers.</param>
    /// <param name="logger">Logger injected into MullaiChatClient for structured tracing.</param>
    public static MullaiChatClient Create(
        string modelsJsonPath,
        IConfiguration configuration,
        ICredentialStorage credentialStorage,
        HttpClient httpClient,
        ILogger<MullaiChatClient> logger)
    {
        var config = LoadModelsConfig(modelsJsonPath);
        var clients = BuildOrderedClients(config, configuration, credentialStorage, httpClient);

        if (clients.Count == 0)
            throw new InvalidOperationException(
                "No enabled providers/models found in models.json. " +
                "Check that at least one provider and model have Enabled=true " +
                "and the corresponding API key is set in appsettings.");

        return new MullaiChatClient(clients, logger);
    }

    private static MullaiProvidersConfig LoadModelsConfig(string modelsJsonPath)
    {
        if (!File.Exists(modelsJsonPath))
            throw new FileNotFoundException($"models.json not found at: {modelsJsonPath}");

        var json = File.ReadAllText(modelsJsonPath);

        // The root key in models.json is "MullaiProviders"
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("MullaiProviders", out var providersElement))
            throw new InvalidOperationException("models.json must have a root 'MullaiProviders' object.");

        return JsonSerializer.Deserialize<MullaiProvidersConfig>(providersElement, _jsonOptions)
               ?? throw new InvalidOperationException("Failed to deserialise MullaiProviders from models.json.");
    }

    private static List<(string Label, IChatClient Client)> BuildOrderedClients(
        MullaiProvidersConfig config,
        IConfiguration configuration,
        ICredentialStorage credentialStorage,
        HttpClient httpClient)
    {
        var result = new List<(string, IChatClient)>();

        var enabledProviders = config.Providers
            .Where(p => p.Enabled && credentialStorage.IsProviderEnabled(p.Name, true))
            .OrderBy(p => p.Priority);

        foreach (var provider in enabledProviders)
        {
            var enabledModels = provider.Models
                .Where(m => m.Enabled && credentialStorage.IsModelEnabled(provider.Name, m.ModelId, true))
                .OrderBy(m => m.Priority);

            foreach (var model in enabledModels)
            {
                var label = $"{provider.Name}/{model.ModelId}";

                var client = TryCreateClient(provider.Name, model.ModelId, configuration, credentialStorage, httpClient);
                if (client is null)
                    continue; // skip if API key missing / provider not configured

                result.Add((label, client));
            }
        }

        return result;
    }

    /// <summary>
    /// Returns null (and skips) when the necessary API key is absent from configuration or storage,
    /// so a missing key for an optional provider doesn't crash startup.
    /// </summary>
    private static IChatClient? TryCreateClient(
        string providerName,
        string modelId,
        IConfiguration configuration,
        ICredentialStorage credentialStorage,
        HttpClient httpClient)
    {
        // Check secure storage first, then appsettings
        var apiKey = credentialStorage.GetApiKey(providerName);
        
        // If we found a key in storage, we need to inject it into a temporary IConfiguration 
        // because the provider factory methods expect IConfiguration.
        // Alternatively, we can update the providers to take the key directly.
        // For now, let's stick to the IConfiguration but overlay the storage key.
        
        var effectiveConfig = apiKey != null 
            ? OverlayApiKey(configuration, providerName, apiKey) 
            : configuration;

        try
        {
            return providerName switch
            {
                "Gemini"      => Gemini.GetGeminiChatClient(effectiveConfig, httpClient, modelId),
                "Groq"        => Groq.GetGroqChatClient(effectiveConfig, httpClient, modelId),
                "Cerebras"    => Cerebras.GetCerebrasChatClient(effectiveConfig, httpClient, modelId),
                "Mistral"     => Mistral.GetMistralChatClient(effectiveConfig, httpClient, modelId),
                "OpenRouter"  => OpenRouter.GetOpenRouterChatClient(effectiveConfig, httpClient, modelId),
                "OllamaOpenAI"=> OllamaOpenAI.GetOllamaOpenAIChatClient(effectiveConfig, httpClient, modelId),
                _ => null
            };
        }
        catch (InvalidOperationException)
        {
            // Missing API key or misconfiguration — skip this provider/model
            return null;
        }
    }

    private static IConfiguration OverlayApiKey(IConfiguration original, string providerName, string apiKey)
    {
        var dict = new Dictionary<string, string?>
        {
            [$"{providerName}:ApiKey"] = apiKey
        };
        
        return new ConfigurationBuilder()
            .AddConfiguration(original)
            .AddInMemoryCollection(dict)
            .Build();
    }
}
