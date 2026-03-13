namespace Mullai.Channels.Core.Models;

public record ChannelMessage
{
    /// <summary>
    /// The original source channel, e.g., "Telegram", "WhatsApp".
    /// </summary>
    public string ChannelId { get; init; } = string.Empty;

    /// <summary>
    /// The identifier for the user or chat in the remote channel.
    /// </summary>
    public string UserId { get; init; } = string.Empty;

    /// <summary>
    /// The textual content of the message.
    /// </summary>
    public string TextContent { get; init; } = string.Empty;

    /// <summary>
    /// The original raw data object for channel-specific parsing if necessary.
    /// </summary>
    public object? RawData { get; init; }
}
