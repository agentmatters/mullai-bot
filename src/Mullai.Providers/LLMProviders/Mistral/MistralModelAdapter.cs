using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mullai.Abstractions.Configuration;
using Mullai.Abstractions.Models;
using Mullai.Providers.Common;

namespace Mullai.Providers.LLMProviders.Mistral;

public class MistralModelAdapter : IModelMetadataAdapter
{
    private const string ModelsEndpoint = "https://api.mistral.ai/v1/models";
    private const string OpenRouterModelsEndpoint = "https://openrouter.ai/api/v1/models";

    public string ProviderName => "Mistral";

    public async Task<List<MullaiModelDescriptor>> FetchModelsAsync(HttpClient httpClient, string? apiKey = null)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            var storage = new FileCredentialStorage();
            apiKey = storage.GetApiKey(ProviderName);
        }

        if (string.IsNullOrEmpty(apiKey)) return new List<MullaiModelDescriptor>();

        // Fetch Mistral models
        using var request = new HttpRequestMessage(HttpMethod.Get, ModelsEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<MistralModelsResponse>();

        if (result?.Data == null) return new List<MullaiModelDescriptor>();

        // Fetch OpenRouter models for pricing
        var orOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        var orResponse =
            await httpClient.GetFromJsonAsync<OpenRouterModelsResponse>(OpenRouterModelsEndpoint, orOptions);
        var openRouterModels = orResponse?.Data ?? new List<OpenRouterPricingModelData>();

        return result.Data.Select(d => Adapt(d, openRouterModels)).ToList();
    }

    private MullaiModelDescriptor Adapt(MistralModelData data, List<OpenRouterPricingModelData> openRouterModels)
    {
        var capabilities = new List<string>();
        if (data.Capabilities?.CompletionChat == true) capabilities.Add("chat");
        if (data.Capabilities?.FunctionCalling == true) capabilities.Add("tools");
        if (data.Capabilities?.Vision == true) capabilities.Add("vision");

        var openRouterMatch = openRouterModels.FirstOrDefault(m =>
            m.Id.Contains($"mistralai/{string.Join("-", data.Id.Split("-").Take(2))}"));

        return new MullaiModelDescriptor
        {
            ModelId = data.Id,
            ModelName = data.Name ?? data.Id,
            Description = data.Description ?? string.Empty,
            ContextWindow =
                data.MaxContextLength, // Assuming ContextLength was a typo and MaxContextLength is correct based on MistralModelData
            Priority = 1,
            Enabled = true,
            Capabilities = capabilities,
            Pricing = openRouterMatch?.Pricing != null
                ? new ModelPricing
                {
                    InputPer1kTokens = ParsePricing(openRouterMatch.Pricing.Prompt),
                    OutputPer1kTokens = ParsePricing(openRouterMatch.Pricing.Completion)
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

internal class MistralModelsResponse
{
    [JsonPropertyName("data")] public List<MistralModelData>? Data { get; set; }
}

internal class MistralModelData
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")] public string? Name { get; set; }

    [JsonPropertyName("description")] public string? Description { get; set; }

    [JsonPropertyName("max_context_length")]
    public int MaxContextLength { get; set; }

    [JsonPropertyName("capabilities")] public MistralCapabilities? Capabilities { get; set; }
}

internal class MistralCapabilities
{
    [JsonPropertyName("completion_chat")] public bool CompletionChat { get; set; }

    [JsonPropertyName("function_calling")] public bool FunctionCalling { get; set; }

    [JsonPropertyName("vision")] public bool Vision { get; set; }
}