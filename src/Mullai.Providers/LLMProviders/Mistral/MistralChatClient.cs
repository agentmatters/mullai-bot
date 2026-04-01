using Mullai.Providers.Common.Http;

namespace Mullai.Providers.LLMProviders.Mistral;

/// <summary>
///     Mistral-specific IChatClient implementation using the generic HTTP base.
/// </summary>
public class MistralChatClient : BaseHttpChatClient<MistralChatRequest, MistralChatResponse, MistralStreamingUpdate>
{
    public MistralChatClient(HttpClient httpClient, Uri endpointUri)
        : base(httpClient, endpointUri, new MistralAdapter(), new MistralConsolidator())
    {
    }

    // Additional Mistral-specific logic can be added here
}