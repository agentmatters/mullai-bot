using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mullai.Abstractions.Models;
using Mullai.Providers.Common;
using Mullai.Abstractions.Configuration;

namespace Mullai.Providers.LLMProviders.Gemini;

public class GeminiModelAdapter : IModelMetadataAdapter
{
    private const string ModelsEndpointTemplate = "https://generativelanguage.googleapis.com/v1beta/models?key={0}";
    private const string OpenRouterModelsEndpoint = "https://openrouter.ai/api/v1/models";

    public string ProviderName => "Gemini";

    public async Task<List<MullaiModelDescriptor>> FetchModelsAsync(HttpClient httpClient, string? apiKey = null)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            var storage = new FileCredentialStorage();
            apiKey = storage.GetApiKey(ProviderName);
        }

        if (string.IsNullOrEmpty(apiKey))
        {
            return new List<MullaiModelDescriptor>();
        }

        // 1. Fetch Gemini models
        var url = string.Format(ModelsEndpointTemplate, apiKey);
        var geminiResponse = await httpClient.GetFromJsonAsync<GeminiModelsResponse>(url);
        var geminiModels = geminiResponse?.Models ?? new List<GeminiModelData>();

        // 2. Fetch OpenRouter models for pricing
        var orOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        var orResponse = await httpClient.GetFromJsonAsync<OpenRouterModelsResponse>(OpenRouterModelsEndpoint, orOptions);
        var openRouterModels = orResponse?.Data ?? new List<OpenRouterPricingModelData>();

        // 3. Adapt and match
        return geminiModels.Select(g => Adapt(g, openRouterModels)).ToList();
    }

    private MullaiModelDescriptor Adapt(GeminiModelData data, List<OpenRouterPricingModelData> openRouterModels)
    {
        // Remove "models/" prefix
        var modelId = data.Name.StartsWith("models/") ? data.Name.Substring("models/".Length) : data.Name;
        
        // Match with openrouter: google/{modelId}
        var orMatch = openRouterModels.FirstOrDefault(m => m.Id == $"google/{modelId}");

        var capabilities = new List<string> { "chat" };
        if (data.SupportedGenerationMethods?.Contains("generateContent") == true) capabilities.Add("generation");

        return new MullaiModelDescriptor
        {
            ModelId = modelId,
            ModelName = data.DisplayName ?? modelId,
            Description = data.Description ?? string.Empty,
            ContextWindow = data.InputTokenLimit,
            Enabled = true,
            Priority = 1,
            Capabilities = capabilities,
            Pricing = orMatch?.Pricing != null ? new ModelPricing
            {
                InputPer1kTokens = ParsePricing(orMatch.Pricing.Prompt),
                OutputPer1kTokens = ParsePricing(orMatch.Pricing.Completion)
            } : null
        };
    }

    private decimal ParsePricing(string? pricing)
    {
        if (string.IsNullOrEmpty(pricing)) return 0;
        if (decimal.TryParse(pricing, out var result))
        {
            return result * 1000000m;
        }
        return 0;
    }
}

internal class GeminiModelsResponse
{
    [JsonPropertyName("models")]
    public List<GeminiModelData>? Models { get; set; }
}

internal class GeminiModelData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("inputTokenLimit")]
    public int InputTokenLimit { get; set; }

    [JsonPropertyName("supportedGenerationMethods")]
    public List<string>? SupportedGenerationMethods { get; set; }
}

internal class OpenRouterModelsResponse
{
    public List<OpenRouterPricingModelData>? Data { get; set; }
}

internal class OpenRouterPricingModelData
{
    public string Id { get; set; } = string.Empty;
    public OpenRouterPricingData? Pricing { get; set; }
}

internal class OpenRouterPricingData
{
    public string? Prompt { get; set; }
    public string? Completion { get; set; }
}
