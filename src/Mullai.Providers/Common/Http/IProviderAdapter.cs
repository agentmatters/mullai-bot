using Microsoft.Extensions.AI;

namespace Mullai.Providers.Common.Http;

/// <summary>
///     Adapts provider-specific Request and Response DTOs to standard Microsoft.Extensions.AI types.
/// </summary>
/// <typeparam name="TRequest">The provider's Request DTO type.</typeparam>
/// <typeparam name="TResponse">The provider's non-streaming Response DTO type.</typeparam>
/// <typeparam name="TStream">The provider's streaming update DTO type.</typeparam>
public interface IProviderAdapter<TRequest, TResponse, TStream>
{
    /// <summary>
    ///     Maps generic messages and options to the provider-specific request.
    /// </summary>
    TRequest MapRequest(IEnumerable<ChatMessage> messages, ChatOptions? options, bool isStreaming);

    /// <summary>
    ///     Maps the provider's non-streaming response to a generic ChatResponse.
    /// </summary>
    ChatResponse MapResponse(TResponse response);

    /// <summary>
    ///     Maps a provider-specific streaming update (single chunk) to a generic ChatResponseUpdate.
    /// </summary>
    ChatResponseUpdate MapStreamingUpdate(TStream streamUpdate);
}