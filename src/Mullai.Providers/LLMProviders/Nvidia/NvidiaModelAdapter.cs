using Mullai.Abstractions.Models;
using Mullai.Providers.Common;

namespace Mullai.Providers.LLMProviders.Nvidia;

public class NvidiaModelAdapter : IModelMetadataAdapter
{
    public string ProviderName => "Mistral";

    public Task<List<MullaiModelDescriptor>> FetchModelsAsync(HttpClient httpClient, string? apiKey = null)
    {
        var models = new List<MullaiModelDescriptor>
        {
            new MullaiModelDescriptor
            {
                ModelId = "qwen/qwen3.5-122b-a10b", 
                ModelName = "qwen/qwen3.5-122b-a10b", 
                Enabled = true, 
                Priority = 1, 
                Capabilities = ["chat"]
            },
        };

        return Task.FromResult(models);
    }
}