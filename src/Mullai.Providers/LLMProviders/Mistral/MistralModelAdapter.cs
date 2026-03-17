using Mullai.Providers.Common;
using Mullai.Abstractions.Models;

namespace Mullai.Providers.LLMProviders.Mistral;

public class MistralModelAdapter : IModelMetadataAdapter
{
    public string ProviderName => "Mistral";

    public Task<List<MullaiModelDescriptor>> FetchModelsAsync(HttpClient httpClient, string? apiKey = null)
    {
        var models = new List<MullaiModelDescriptor>
        {
            new MullaiModelDescriptor { ModelId = "mistral-medium-2505", ModelName = "mistral-medium-2505", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "mistral-medium-2508", ModelName = "mistral-medium-2508", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "mistral-medium-latest", ModelName = "mistral-medium-2508", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "mistral-medium", ModelName = "mistral-medium-2508", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "mistral-vibe-cli-with-tools", ModelName = "mistral-medium-2508", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "open-mistral-nemo", ModelName = "open-mistral-nemo", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "open-mistral-nemo-2407", ModelName = "open-mistral-nemo", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "mistral-tiny-2407", ModelName = "open-mistral-nemo", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "mistral-tiny-latest", ModelName = "open-mistral-nemo", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "codestral-2508", ModelName = "codestral-2508", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "codestral-latest", ModelName = "codestral-2508", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "devstral-2512", ModelName = "devstral-2512", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "mistral-vibe-cli-latest", ModelName = "devstral-2512", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "devstral-medium-latest", ModelName = "devstral-2512", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "devstral-latest", ModelName = "devstral-2512", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "mistral-small-2603", ModelName = "mistral-small-2603", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "mistral-small-latest", ModelName = "mistral-small-2603", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "mistral-small-2506", ModelName = "mistral-small-2506", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "labs-mistral-small-creative", ModelName = "labs-mistral-small-creative", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "magistral-medium-2509", ModelName = "magistral-medium-2509", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "magistral-medium-latest", ModelName = "magistral-medium-2509", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "magistral-small-2509", ModelName = "magistral-small-2509", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "magistral-small-latest", ModelName = "magistral-small-2509", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "voxtral-small-2507", ModelName = "voxtral-small-2507", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "voxtral-small-latest", ModelName = "voxtral-small-2507", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "labs-leanstral-2603", ModelName = "labs-leanstral-2603", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "mistral-large-2512", ModelName = "mistral-large-2512", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "mistral-large-latest", ModelName = "mistral-large-2512", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "ministral-3b-2512", ModelName = "ministral-3b-2512", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "ministral-3b-latest", ModelName = "ministral-3b-2512", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "ministral-8b-2512", ModelName = "ministral-8b-2512", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "ministral-8b-latest", ModelName = "ministral-8b-2512", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "ministral-14b-2512", ModelName = "ministral-14b-2512", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "ministral-14b-latest", ModelName = "ministral-14b-2512", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "mistral-large-2411", ModelName = "mistral-large-2411", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "pixtral-large-2411", ModelName = "pixtral-large-2411", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "pixtral-large-latest", ModelName = "pixtral-large-2411", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "mistral-large-pixtral-2411", ModelName = "pixtral-large-2411", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "devstral-small-2507", ModelName = "devstral-small-2507", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "devstral-medium-2507", ModelName = "devstral-medium-2507", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "labs-devstral-small-2512", ModelName = "labs-devstral-small-2512", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "devstral-small-latest", ModelName = "labs-devstral-small-2512", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "voxtral-mini-2507", ModelName = "voxtral-mini-2507", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "voxtral-mini-latest", ModelName = "voxtral-mini-2507", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "mistral-squarepoint-2602", ModelName = "mistral-squarepoint-2602", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "mistral-embed-2312", ModelName = "mistral-embed-2312", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "mistral-embed", ModelName = "mistral-embed-2312", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "codestral-embed", ModelName = "codestral-embed", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "codestral-embed-2505", ModelName = "codestral-embed", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "mistral-moderation-2411", ModelName = "mistral-moderation-2411", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "mistral-moderation-latest", ModelName = "mistral-moderation-2411", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "mistral-ocr-2512", ModelName = "mistral-ocr-2512", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "mistral-ocr-latest", ModelName = "mistral-ocr-2512", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "mistral-ocr-2503", ModelName = "mistral-ocr-2503", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "mistral-ocr-2505", ModelName = "mistral-ocr-2505", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "voxtral-mini-2602", ModelName = "voxtral-mini-2602", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "voxtral-mini-latest", ModelName = "voxtral-mini-2602", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "voxtral-mini-transcribe-realtime-2602", ModelName = "voxtral-mini-transcribe-realtime-2602", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "voxtral-mini-realtime-2602", ModelName = "voxtral-mini-transcribe-realtime-2602", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "voxtral-mini-realtime-latest", ModelName = "voxtral-mini-transcribe-realtime-2602", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "voxtral-mini-transcribe-2507", ModelName = "voxtral-mini-transcribe-2507", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "voxtral-mini-2507", ModelName = "voxtral-mini-transcribe-2507", Enabled = true, Priority = 1, Capabilities = ["chat"] },
            new MullaiModelDescriptor { ModelId = "voxtral-mini-tts-260213", ModelName = "voxtral-mini-tts-260213", Enabled = true, Priority = 1, Capabilities = ["chat"] }
        };

        return Task.FromResult(models);
    }
}
