using System.Net.Http.Json;
using Mullai.Providers.Common;
using Mullai.Abstractions.Models;

namespace Mullai.Providers.LLMProviders.OpenRouter;

public class OpenRouterModelAdapter : IModelMetadataAdapter
{
    private const string ModelsEndpoint = "https://openrouter.ai/api/v1/models";

    public string ProviderName => "OpenRouter";

    public async Task<List<MullaiModelDescriptor>> FetchModelsAsync(HttpClient httpClient, string? apiKey = null)
    {
        var response = await httpClient.GetFromJsonAsync<OpenRouterModelsResponse>(ModelsEndpoint);
        
        if (response?.Data == null)
        {
            return new List<MullaiModelDescriptor>();
        }

        return response.Data.Select(Adapt).ToList();
    }

    private MullaiModelDescriptor Adapt(OpenRouterModelData data)
    {
        return new MullaiModelDescriptor
        {
            ModelId = data.Id,
            ModelName = data.Name,
            Description = data.Description,
            ContextWindow = data.ContextLength,
            Priority = 10, // Default dynamic priority
            Enabled = true,
            Capabilities = new List<string> { "chat" }, // Assume chat for now
            Pricing = data.Pricing != null ? new ModelPricing
            {
                // OpenRouter pricing is per token? Or per 1M tokens?
                // The sample response shows "0.00000096" for prompt.
                // Mullai wants per 1k tokens.
                // 0.00000096 * 1000 = 0.00096
                InputPer1kTokens = ParsePricing(data.Pricing.Prompt),
                OutputPer1kTokens = ParsePricing(data.Pricing.Completion)
            } : null
        };
    }

    private decimal ParsePricing(string? pricing)
    {
        if (string.IsNullOrEmpty(pricing)) return 0;
        if (decimal.TryParse(pricing, out var result))
        {
            return result * 1000m;
        }
        return 0;
    }
}

internal class OpenRouterModelsResponse
{
    public List<OpenRouterModelData>? Data { get; set; }
}

internal class OpenRouterModelData
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
    public string? Request { get; set; }
    public string? Image { get; set; }
}
