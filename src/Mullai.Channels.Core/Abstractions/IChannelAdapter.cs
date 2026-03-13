using Mullai.Channels.Core.Models;

namespace Mullai.Channels.Core.Abstractions;

public interface IChannelAdapter
{
    /// <summary>
    /// The unique identifier for this channel (e.g., "Telegram").
    /// </summary>
    string ChannelId { get; }

    /// <summary>
    /// Event triggered when a message is received from the channel.
    /// </summary>
    event Func<ChannelMessage, Task>? OnMessageReceived;

    /// <summary>
    /// Sends a response message back to the specified user/chat on the channel.
    /// </summary>
    /// <param name="response">The message to send.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendMessageAsync(ChannelMessage response);
    
    /// <summary>
    /// Processes an incoming raw request (useful for Webhooks).
    /// </summary>
    /// <param name="requestData">The raw payload from the webhook.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ProcessIncomingMessageAsync(object requestData);
}
