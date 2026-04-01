using System.Net.Http.Json;
using System.Text.Json;
using Mullai.Abstractions.Models;
using Mullai.Providers.Common;

namespace Mullai.Providers.LLMProviders.Cerebras;

public class CerebrasModelAdapter : IModelMetadataAdapter
{
    private const string ModelsEndpoint = "https://api.cerebras.ai/public/v1/models";

    public string ProviderName => "Cerebras";

    public async Task<List<MullaiModelDescriptor>> FetchModelsAsync(HttpClient httpClient, string? apiKey = null)
    {
        // No auth required for models endpoint according to user
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        var response = await httpClient.GetFromJsonAsync<CerebrasModelsResponse>(ModelsEndpoint, options);

        if (response?.Data == null) return new List<MullaiModelDescriptor>();

        return response.Data.Select(Adapt).ToList();
    }

    private MullaiModelDescriptor Adapt(CerebrasModelData data)
    {
        var capabilities = new List<string> { "chat" };
        if (data.Capabilities?.Streaming == true) capabilities.Add("streaming");
        if (data.Capabilities?.Tools == true || data.Capabilities?.FunctionCalling == true) capabilities.Add("tools");
        if (data.Capabilities?.Vision == true) capabilities.Add("vision");

        return new MullaiModelDescriptor
        {
            ModelId = data.Id,
            ModelName = data.Name ?? data.Id,
            Description = data.Description ?? string.Empty,
            ContextWindow = data.Limits?.MaxContextLength ?? 0,
            Enabled = true,
            Priority = 1,
            Capabilities = capabilities,
            Pricing = data.Pricing != null
                ? new ModelPricing
                {
                    InputPer1kTokens = ParsePricing(data.Pricing.Prompt),
                    OutputPer1kTokens = ParsePricing(data.Pricing.Completion)
                }
                : null
        };
    }

    private decimal ParsePricing(string? pricing)
    {
        if (string.IsNullOrEmpty(pricing)) return 0;
        if (decimal.TryParse(pricing, out var result))
            // Assuming the pricing is per token in the response, Mullai wants per 1M tokens
            return result * 1000000m;
        return 0;
    }
}

internal class CerebrasModelsResponse
{
    public List<CerebrasModelData>? Data { get; set; }
}

internal class CerebrasModelData
{
    public string Id { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Description { get; set; }
    public CerebrasPricingData? Pricing { get; set; }
    public CerebrasCapabilities? Capabilities { get; set; }
    public CerebrasLimits? Limits { get; set; }
}

internal class CerebrasPricingData
{
    public string? Prompt { get; set; }
    public string? Completion { get; set; }
}

internal class CerebrasCapabilities
{
    public bool Streaming { get; set; }
    public bool FunctionCalling { get; set; }
    public bool Tools { get; set; }
    public bool Vision { get; set; }
}

internal class CerebrasLimits
{
    public int MaxContextLength { get; set; }
}