using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mullai.Abstractions.Models;
using Mullai.Providers.Common;

namespace Mullai.Providers.LLMProviders.Nvidia;

public class NvidiaModelAdapter : IModelMetadataAdapter
{
    private const string ModelsEndpoint = "https://integrate.api.nvidia.com/v1/models";
    private const string OpenRouterModelsEndpoint = "https://openrouter.ai/api/v1/models";

    public string ProviderName => "Nvidia";

    public async Task<List<MullaiModelDescriptor>> FetchModelsAsync(HttpClient httpClient, string? apiKey = null)
    {
        // 1. Fetch NVIDIA models
        var nvidiaResponse = await httpClient.GetFromJsonAsync<NvidiaModelsResponse>(ModelsEndpoint);
        var nvidiaModels = nvidiaResponse?.Data ?? new List<NvidiaModelData>();

        // 2. Fetch OpenRouter models for pricing/metadata
        var orOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        var orResponse = await httpClient.GetFromJsonAsync<OpenRouterModelsResponse>(OpenRouterModelsEndpoint, orOptions);
        var openRouterModels = orResponse?.Data ?? new List<OpenRouterPricingModelData>();

        // 3. Adapt and match
        return nvidiaModels.Select(n => Adapt(n, openRouterModels)).ToList();
    }

    private MullaiModelDescriptor Adapt(NvidiaModelData data, List<OpenRouterPricingModelData> openRouterModels)
    {
        var orMatch = openRouterModels.FirstOrDefault(m => m.HuggingFaceId == data.Id);

        return new MullaiModelDescriptor
        {
            ModelId = data.Id,
            ModelName = orMatch?.Name ?? data.Id,
            Description = orMatch?.Description ?? string.Empty,
            ContextWindow = orMatch?.ContextLength ?? 0,
            Enabled = true,
            Priority = 1,
            Capabilities = ["chat"],
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

internal class NvidiaModelsResponse
{
    [JsonPropertyName("data")]
    public List<NvidiaModelData>? Data { get; set; }
}

internal class NvidiaModelData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
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
    public string? HuggingFaceId { get; set; }
    public OpenRouterPricingData? Pricing { get; set; }
}

internal class OpenRouterPricingData
{
    public string? Prompt { get; set; }
    public string? Completion { get; set; }
}