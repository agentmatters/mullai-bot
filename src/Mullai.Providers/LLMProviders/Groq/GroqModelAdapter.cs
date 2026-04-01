using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mullai.Abstractions.Configuration;
using Mullai.Abstractions.Models;
using Mullai.Providers.Common;

namespace Mullai.Providers.LLMProviders.Groq;

public class GroqModelAdapter : IModelMetadataAdapter
{
    private const string ModelsEndpoint = "https://api.groq.com/openai/v1/models";
    private const string OpenRouterModelsEndpoint = "https://openrouter.ai/api/v1/models";

    public string ProviderName => "Groq";

    public async Task<List<MullaiModelDescriptor>> FetchModelsAsync(HttpClient httpClient, string? apiKey = null)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            var storage = new FileCredentialStorage();
            apiKey = storage.GetApiKey(ProviderName);
        }

        if (string.IsNullOrEmpty(apiKey)) return new List<MullaiModelDescriptor>();

        // 1. Fetch Groq models
        using var request = new HttpRequestMessage(HttpMethod.Get, ModelsEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var groqResponse = await response.Content.ReadFromJsonAsync<GroqModelsResponse>();
        var groqModels = groqResponse?.Data ?? new List<GroqModelData>();

        // 2. Fetch OpenRouter models for pricing
        var orOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        var orResponse =
            await httpClient.GetFromJsonAsync<OpenRouterModelsResponse>(OpenRouterModelsEndpoint, orOptions);
        var openRouterModels = orResponse?.Data ?? new List<OpenRouterPricingModelData>();

        // 3. Adapt and match
        return groqModels.Select(g => Adapt(g, openRouterModels)).ToList();
    }

    private MullaiModelDescriptor Adapt(GroqModelData data, List<OpenRouterPricingModelData> openRouterModels)
    {
        var orMatch = openRouterModels.FirstOrDefault(m => m.Id == data.Id);

        return new MullaiModelDescriptor
        {
            ModelId = data.Id,
            ModelName = orMatch?.Name ?? data.Id,
            Description = orMatch?.Description ?? string.Empty,
            ContextWindow = data.ContextWindow > 0 ? data.ContextWindow : orMatch?.ContextLength ?? 0,
            Enabled = true,
            Priority = 1,
            Capabilities = ["chat"],
            Pricing = orMatch?.Pricing != null
                ? new ModelPricing
                {
                    InputPer1kTokens = ParsePricing(orMatch.Pricing.Prompt),
                    OutputPer1kTokens = ParsePricing(orMatch.Pricing.Completion)
                }
                : null
        };
    }

    private decimal ParsePricing(string? pricing)
    {
        if (string.IsNullOrEmpty(pricing)) return 0;
        if (decimal.TryParse(pricing, out var result)) return result * 1000000m;
        return 0;
    }
}

internal class GroqModelsResponse
{
    [JsonPropertyName("data")] public List<GroqModelData>? Data { get; set; }
}

internal class GroqModelData
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;

    [JsonPropertyName("context_window")] public int ContextWindow { get; set; }
}

internal class OpenRouterModelsResponse
{
    public List<OpenRouterPricingModelData>? Data { get; set; }
}

internal class OpenRouterPricingModelData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ContextLength { get; set; }
    public OpenRouterPricingData? Pricing { get; set; }
}

internal class OpenRouterPricingData
{
    public string? Prompt { get; set; }
    public string? Completion { get; set; }
}