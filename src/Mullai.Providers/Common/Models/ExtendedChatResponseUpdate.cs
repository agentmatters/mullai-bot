using Microsoft.Extensions.AI;

namespace Mullai.Providers.Common.Models;

/// <summary>
///     An extension of ChatResponseUpdate to support provider-specific fields like "Thinking" or "Reasoning".
/// </summary>
public class ExtendedChatResponseUpdate : ChatResponseUpdate
{
    /// <summary>
    ///     Optional thinking/reasoning content provided by some models (e.g., Mistral, DeepSeek).
    /// </summary>
    public string? Thinking { get; set; }

    public override string ToString()
    {
        return Thinking ?? base.ToString() ?? string.Empty;
    }
}