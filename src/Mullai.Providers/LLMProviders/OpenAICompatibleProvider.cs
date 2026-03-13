using System;
using System.ClientModel;
using System.Net.Http;
using System.ClientModel.Primitives;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Mullai.OpenTelemetry.OpenTelemetry;
using OpenAI;

namespace Mullai.Providers.LLMProviders;

public static class OpenAICompatibleProvider
{
    public static IChatClient CreateChatClient(
        string providerName,
        string defaultEndpoint,
        IConfiguration configuration,
        HttpClient httpClient,
        Action<ChatClientBuilder>? configureBuilder = null,
        string? modelIdOverride = null)
    {
        var endpoint = configuration[$"{providerName}:Endpoint"] ?? defaultEndpoint;
        var modelId = modelIdOverride ?? configuration[$"{providerName}:ModelId"];
        var apiKey = configuration[$"{providerName}:ApiKey"];

        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new InvalidOperationException($"{providerName}:ModelId is missing from configuration and no modelIdOverride was supplied.");
        }
            
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException($"{providerName}:ApiKey is missing from configuration.");
        }

        var openAIOptions = new OpenAIClientOptions()
        {
            Endpoint = new Uri(endpoint),
            Transport = new HttpClientPipelineTransport(httpClient)
        };

        var openAIClient = new OpenAIClient(new ApiKeyCredential(apiKey), openAIOptions);
        
        var builder = openAIClient.GetChatClient(modelId)
            .AsIChatClient()
            .AsBuilder()
            .UseOpenTelemetry(
                sourceName: OpenTelemetrySettings.ServiceName, 
                configure: (cfg) => cfg.EnableSensitiveData = true);
                
        configureBuilder?.Invoke(builder);
        
        return builder.Build();
    }
}
