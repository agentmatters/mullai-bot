using Mullai.Abstractions.Models;

namespace Mullai.Providers.Common;

/// <summary>
/// Interface for fetching and adapting model metadata from various providers.
/// </summary>
public interface IModelMetadataAdapter
{
    /// <summary>
    /// The name of the provider this adapter handles (e.g., "OpenRouter").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Fetches the list of available models from the provider and returns them as <see cref="MullaiModelDescriptor"/> objects.
    /// </summary>
    /// <param name="httpClient">The HttpClient to use for the request.</param>
    /// <param name="apiKey">The API key for the provider.</param>
    /// <returns>A list of model descriptors.</returns>
    Task<List<MullaiModelDescriptor>> FetchModelsAsync(HttpClient httpClient, string? apiKey = null);
}
