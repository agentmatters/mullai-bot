using Microsoft.Extensions.Logging;
using Mullai.Agents;
using Mullai.Channels.Core.Clients;
using Mullai.Channels.Core.Abstractions;
using Mullai.Channels.Core.Models;
using System.Collections.Concurrent;

namespace Mullai.Channels.Core;

public class ChannelManager
{
    private readonly IEnumerable<IChannelAdapter> _channelAdapters;
    private readonly AgentFactory _agentFactory;
    private readonly ILogger<ChannelManager> _logger;
    private readonly ConcurrentDictionary<string, ChannelMullaiClient> _clients;

    public ChannelManager(
        IEnumerable<IChannelAdapter> channelAdapters,
        AgentFactory agentFactory,
        ILogger<ChannelManager> logger)
    {
        _channelAdapters = channelAdapters ?? throw new ArgumentNullException(nameof(channelAdapters));
        _agentFactory = agentFactory ?? throw new ArgumentNullException(nameof(agentFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _clients = new ConcurrentDictionary<string, ChannelMullaiClient>();

        InitializeAdapters();
    }

    private void InitializeAdapters()
    {
        foreach (var adapter in _channelAdapters)
        {
            adapter.OnMessageReceived += HandleIncomingMessageAsync;
            _logger.LogInformation("Initialized channel adapter: {ChannelId}", adapter.ChannelId);
        }
    }

    private async Task HandleIncomingMessageAsync(ChannelMessage message)
    {
        try
        {
            _logger.LogInformation("Received message on {ChannelId} from {UserId}", message.ChannelId, message.UserId);

            // Using the Assistant as default. We can make this configurable later based on ChannelId or UserId
            var client = _clients.GetOrAdd(message.UserId, _ => new ChannelMullaiClient(_agentFactory));

            string fullResponse = "";
            
            // We await the textual response completely before returning it
            // as most chat platforms are not built for character-by-character streaming webhook repsonses
            fullResponse = await client.RunAsync(message.TextContent);

            var responseMessage = new ChannelMessage
            {
                ChannelId = message.ChannelId,
                UserId = message.UserId,
                TextContent = fullResponse
            };

            var adapter = _channelAdapters.FirstOrDefault(a => a.ChannelId == message.ChannelId);
            if (adapter != null)
            {
                await adapter.SendMessageAsync(responseMessage);
                _logger.LogInformation("Sent response to {UserId} on {ChannelId}", message.UserId, message.ChannelId);
            }
            else
            {
                _logger.LogWarning("Adapter not found for ChannelId: {ChannelId}", message.ChannelId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message from {UserId} on {ChannelId}", message.UserId, message.ChannelId);
            // Optionally, we could send a generic error message back to the channel here
        }
    }

    public IChannelAdapter? GetAdapter(string channelId)
    {
        return _channelAdapters.FirstOrDefault(a => string.Equals(a.ChannelId, channelId, StringComparison.OrdinalIgnoreCase));
    }
}
