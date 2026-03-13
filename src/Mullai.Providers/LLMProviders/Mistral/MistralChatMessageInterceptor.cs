using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace Mullai.Providers.LLMProviders.Mistral;

/// <summary>
/// Intercepts messages sent to Mistral API and removes properties that are not supported by Mistral.
/// For example, the Mistral API rejects the 'name' property on message payloads.
/// </summary>
public class MistralChatMessageInterceptor : DelegatingChatClient
{
    public MistralChatMessageInterceptor(IChatClient innerClient) : base(innerClient)
    {
    }

    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages, 
        ChatOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        ProcessMessages(chatMessages);
        return base.GetResponseAsync(chatMessages, options, cancellationToken);
    }

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages, 
        ChatOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        ProcessMessages(chatMessages);
        return base.GetStreamingResponseAsync(chatMessages, options, cancellationToken);
    }

    private void ProcessMessages(IEnumerable<ChatMessage> chatMessages)
    {
        if (chatMessages == null) return;
        
        foreach (var message in chatMessages)
        {
            // Mistral API does not support the 'name' property (AuthorName in Microsoft.Extensions.AI).
            // It will throw a validation error if this property is sent in the payload.
            message.AuthorName = null;
        }
    }
}
