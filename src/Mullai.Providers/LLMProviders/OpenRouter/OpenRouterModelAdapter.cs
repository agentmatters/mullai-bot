using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mullai.Abstractions.Models;
using Mullai.Providers.Common;

namespace Mullai.Providers.LLMProviders.OpenRouter;

public class OpenRouterModelAdapter : IModelMetadataAdapter
{
    private const string ModelsEndpoint = "https://openrouter.ai/api/v1/models";

    public string ProviderName => "OpenRouter";

    public async Task<List<MullaiModelDescriptor>> FetchModelsAsync(HttpClient httpClient, string? apiKey = null)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        var response = await httpClient.GetFromJsonAsync<OpenRouterModelsResponse>(
            ModelsEndpoint, options);

        if (response?.Data == null) return new List<MullaiModelDescriptor>();

        return response.Data.Select(Adapt).ToList();
    }

    private MullaiModelDescriptor Adapt(OpenRouterModelData data)
    {
        return new MullaiModelDescriptor
        {
            ModelId = data.Id,
            ModelName = data.Name,
            Description = data.Description ?? string.Empty,
            ContextWindow = data.ContextLength,
            Priority = 10, // Default dynamic priority
            Enabled = true,
            Capabilities = new List<string> { "chat" }, // Assume chat for now
            Pricing = data.Pricing != null
                ? new ModelPricing
                {
                    // OpenRouter pricing is per token? Or per 1M tokens?
                    // The sample response shows "0.00000096" for prompt.
                    // Mullai wants per 1M tokens.
                    // 0.00000096 * 100000 = 0.00096
                    InputPer1kTokens = ParsePricing(data.Pricing.Prompt),
                    OutputPer1kTokens = ParsePricing(data.Pricing.Completion)
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
    public List<OpenRouterModelData>? Data { get; set; }
}

internal class OpenRouterModelData
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")] public string? Description { get; set; }

    [JsonPropertyName("context_length")] public int ContextLength { get; set; }

    [JsonPropertyName("canonical_slug")] public string? CanonicalSlug { get; set; }

    [JsonPropertyName("created")] public long Created { get; set; }

    [JsonPropertyName("architecture")] public OpenRouterArchitecture? Architecture { get; set; }

    [JsonPropertyName("pricing")] public OpenRouterPricingData? Pricing { get; set; }

    [JsonPropertyName("top_provider")] public OpenRouterTopProvider? TopProvider { get; set; }

    [JsonPropertyName("supported_parameters")]
    public List<string>? SupportedParameters { get; set; }

    [JsonPropertyName("default_parameters")]
    public OpenRouterDefaultParameters? DefaultParameters { get; set; }
}

internal class OpenRouterArchitecture
{
    [JsonPropertyName("modality")] public string? Modality { get; set; }

    [JsonPropertyName("input_modalities")] public List<string>? InputModalities { get; set; }

    [JsonPropertyName("output_modalities")]
    public List<string>? OutputModalities { get; set; }

    [JsonPropertyName("tokenizer")] public string? Tokenizer { get; set; }

    [JsonPropertyName("instruct_type")] public string? InstructType { get; set; }
}

internal class OpenRouterPricingData
{
    [JsonPropertyName("prompt")] public string? Prompt { get; set; }

    [JsonPropertyName("completion")] public string? Completion { get; set; }

    [JsonPropertyName("request")] public string? Request { get; set; }

    [JsonPropertyName("image")] public string? Image { get; set; }

    [JsonPropertyName("input_cache_read")] public string? InputCacheRead { get; set; }
}

internal class OpenRouterTopProvider
{
    [JsonPropertyName("context_length")] public int? ContextLength { get; set; }

    [JsonPropertyName("max_completion_tokens")]
    public int? MaxCompletionTokens { get; set; }

    [JsonPropertyName("is_moderated")] public bool IsModerated { get; set; }
}

internal class OpenRouterDefaultParameters
{
    [JsonPropertyName("temperature")] public double? Temperature { get; set; }

    [JsonPropertyName("top_p")] public double? TopP { get; set; }

    [JsonPropertyName("top_k")] public int? TopK { get; set; }

    [JsonPropertyName("frequency_penalty")]
    public double? FrequencyPenalty { get; set; }

    [JsonPropertyName("presence_penalty")] public double? PresencePenalty { get; set; }

    [JsonPropertyName("repetition_penalty")]
    public double? RepetitionPenalty { get; set; }
}