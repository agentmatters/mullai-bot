using System.Collections.Generic;
using System.Linq;
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
        var messages = ProcessMessages(chatMessages, options);
        return base.GetResponseAsync(messages, options, cancellationToken);
    }

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages, 
        ChatOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        var messages = ProcessMessages(chatMessages, options);
        return base.GetStreamingResponseAsync(messages, options, cancellationToken);
    }

    private IEnumerable<ChatMessage> ProcessMessages(IEnumerable<ChatMessage> chatMessages, ChatOptions? options)
    {
        if (chatMessages == null) return Enumerable.Empty<ChatMessage>();

        var messages = chatMessages;

        // Mistral API requires instructions (if any) to be passed as a system prompt in the messages.
        if (!string.IsNullOrEmpty(options?.Instructions))
        {
            var instructionsMessage = new ChatMessage(ChatRole.System, options.Instructions);
            messages = new[] { instructionsMessage }.Concat(chatMessages);
        }
        
        foreach (var message in messages)
        {
            // Mistral API does not support the 'name' property (AuthorName in Microsoft.Extensions.AI).
            // It will throw a validation error if this property is sent in the payload.
            message.AuthorName = null;
        }

        return messages;
    }
}
