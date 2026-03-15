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
    /// Creates a <see cref="MullaiChatClient"/> from the given configuration.
    /// </summary>
    /// <param name="configuration">appsettings configuration (for API keys).</param>
    /// <param name="credentialStorage">Secure storage for API keys.</param>
    /// <param name="httpClient">Shared HttpClient for OpenAI-compatible providers.</param>
    /// <param name="logger">Logger injected into MullaiChatClient for structured tracing.</param>
    public static MullaiChatClient Create(
        IConfiguration configuration,
        ICredentialStorage credentialStorage,
        HttpClient httpClient,
        ILogger<MullaiChatClient> logger)
    {
        var config = GetHardcodedConfig();
        var clients = BuildOrderedClients(config, configuration, credentialStorage, httpClient);

        if (clients.Count == 0)
            throw new InvalidOperationException(
                "No enabled providers/models found in models.json. " +
                "Check that at least one provider and model have Enabled=true " +
                "and the corresponding API key is set in appsettings.");

        return new MullaiChatClient(clients, logger);
    }

    public static MullaiProvidersConfig GetHardcodedConfig()
    {
        return new MullaiProvidersConfig
        {
            Providers = [
                new MullaiProviderDescriptor
                {
                    Name = "Gemini",
                    Priority = 3,
                    Enabled = false,
                    Models = [
                        new MullaiModelDescriptor
                        {
                            ModelId = "gemini-2.5-flash",
                            ModelName = "Gemini 2.5 Flash",
                            Priority = 1,
                            Enabled = true,
                            Capabilities = ["chat", "vision", "tool_use"],
                            Pricing = new ModelPricing { InputPer1kTokens = 0.00015m, OutputPer1kTokens = 0.0006m },
                            ContextWindow = 1048576
                        },
                        new MullaiModelDescriptor
                        {
                            ModelId = "gemini-2.0-flash",
                            ModelName = "Gemini 2.0 Flash",
                            Priority = 2,
                            Enabled = true,
                            Capabilities = ["chat", "vision", "tool_use"],
                            Pricing = new ModelPricing { InputPer1kTokens = 0.0001m, OutputPer1kTokens = 0.0004m },
                            ContextWindow = 1048576
                        },
                        new MullaiModelDescriptor
                        {
                            ModelId = "gemini-2.0-flash-lite",
                            ModelName = "Gemini 2.0 Flash Lite",
                            Priority = 3,
                            Enabled = true,
                            Capabilities = ["chat", "vision"],
                            Pricing = new ModelPricing { InputPer1kTokens = 0.000075m, OutputPer1kTokens = 0.0003m },
                            ContextWindow = 1048576
                        }
                    ]
                },
                new MullaiProviderDescriptor
                {
                    Name = "Groq",
                    Priority = 2,
                    Enabled = false,
                    Models = [
                        new MullaiModelDescriptor
                        {
                            ModelId = "llama-3.3-70b-versatile",
                            ModelName = "LLaMA 3.3 70B Versatile",
                            Priority = 1,
                            Enabled = true,
                            Capabilities = ["chat", "tool_use"],
                            Pricing = new ModelPricing { InputPer1kTokens = 0.00059m, OutputPer1kTokens = 0.00079m },
                            ContextWindow = 128000
                        },
                        new MullaiModelDescriptor
                        {
                            ModelId = "llama-3.1-8b-instant",
                            ModelName = "LLaMA 3.1 8B Instant",
                            Priority = 2,
                            Enabled = true,
                            Capabilities = ["chat"],
                            Pricing = new ModelPricing { InputPer1kTokens = 0.00005m, OutputPer1kTokens = 0.00008m },
                            ContextWindow = 128000
                        },
                        new MullaiModelDescriptor
                        {
                            ModelId = "meta-llama/llama-4-maverick-17b-128e-instruct",
                            ModelName = "LLaMA 4 Maverick 17B",
                            Priority = 3,
                            Enabled = true,
                            Capabilities = ["chat", "vision"],
                            Pricing = new ModelPricing { InputPer1kTokens = 0.0002m, OutputPer1kTokens = 0.0006m },
                            ContextWindow = 524288
                        }
                    ]
                },
                new MullaiProviderDescriptor
                {
                    Name = "Cerebras",
                    Priority = 1,
                    Enabled = false,
                    Models = [
                        new MullaiModelDescriptor
                        {
                            ModelId = "gpt-oss-120b",
                            ModelName = "GPT-OSS 120B",
                            Priority = 1,
                            Enabled = true,
                            Capabilities = ["chat", "tool_use"],
                            Pricing = new ModelPricing { InputPer1kTokens = 0.0006m, OutputPer1kTokens = 0.0006m },
                            ContextWindow = 128000
                        },
                        new MullaiModelDescriptor
                        {
                            ModelId = "llama3.1-8b",
                            ModelName = "LLaMA 3.1 8B (Cerebras)",
                            Priority = 2,
                            Enabled = true,
                            Capabilities = ["chat"],
                            Pricing = new ModelPricing { InputPer1kTokens = 0.0001m, OutputPer1kTokens = 0.0001m },
                            ContextWindow = 128000
                        },
                        new MullaiModelDescriptor
                        {
                            ModelId = "qwen-3-235b-a22b-instruct-2507",
                            ModelName = "Qwen 3 235B",
                            Priority = 3,
                            Enabled = true,
                            Capabilities = ["chat", "tool_use"],
                            Pricing = new ModelPricing { InputPer1kTokens = 0.0004m, OutputPer1kTokens = 0.0004m },
                            ContextWindow = 32768
                        }
                    ]
                },
                new MullaiProviderDescriptor
                {
                    Name = "Mistral",
                    Priority = 4,
                    Enabled = true,
                    Models = [
                        new MullaiModelDescriptor
                        {
                            ModelId = "mistral-medium-latest",
                            ModelName = "Mistral Medium (latest)",
                            Priority = 1,
                            Enabled = true,
                            Capabilities = ["chat", "tool_use"],
                            Pricing = new ModelPricing { InputPer1kTokens = 0.0027m, OutputPer1kTokens = 0.0081m },
                            ContextWindow = 131072
                        },
                        new MullaiModelDescriptor
                        {
                            ModelId = "mistral-small-latest",
                            ModelName = "Mistral Small (latest)",
                            Priority = 2,
                            Enabled = true,
                            Capabilities = ["chat", "tool_use"],
                            Pricing = new ModelPricing { InputPer1kTokens = 0.0002m, OutputPer1kTokens = 0.0006m },
                            ContextWindow = 131072
                        }
                    ]
                },
                new MullaiProviderDescriptor
                {
                    Name = "OpenRouter",
                    Priority = 5,
                    Enabled = false,
                    Models = [
                        new MullaiModelDescriptor
                        {
                            ModelId = "arcee-ai/trinity-large-preview:free",
                            ModelName = "Arcee Trinity Large (free)",
                            Priority = 1,
                            Enabled = true,
                            Capabilities = ["chat"],
                            Pricing = new ModelPricing { InputPer1kTokens = 0.0m, OutputPer1kTokens = 0.0m },
                            ContextWindow = 32768
                        }
                    ]
                },
                new MullaiProviderDescriptor
                {
                    Name = "OllamaOpenAI",
                    Priority = 6,
                    Enabled = false,
                    Models = [
                        new MullaiModelDescriptor
                        {
                            ModelId = "llama3",
                            ModelName = "LLaMA 3 (local)",
                            Priority = 1,
                            Enabled = true,
                            Capabilities = ["chat"],
                            ContextWindow = 8192
                        }
                    ]
                }
            ]
        };
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
