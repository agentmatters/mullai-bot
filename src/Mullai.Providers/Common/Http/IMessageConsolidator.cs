using Microsoft.Extensions.AI;

namespace Mullai.Providers.Common.Http;

/// <summary>
///     Defines rules for consolidating and pre-processing messages before they are sent to the provider.
/// </summary>
public interface IMessageConsolidator
{
    /// <summary>
    ///     Consolidates a sequence of messages based on provider-specific rules.
    /// </summary>
    IEnumerable<ChatMessage> Consolidate(IEnumerable<ChatMessage> messages);
}