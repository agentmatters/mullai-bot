using System.Collections.Concurrent;
using System.Threading.Channels;
using Mullai.Abstractions.Messaging;

namespace Mullai.Execution.Messaging;

public class InternalEventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, object> _channels = new();
    private readonly Channel<object> _unifiedChannel = Channel.CreateUnbounded<object>(new UnboundedChannelOptions 
    { 
        SingleReader = false, 
        SingleWriter = false 
    });

    public async ValueTask PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
    {
        // Write to type-specific channel (for backward compatibility)
        var channel = GetChannel<T>();
        await channel.Writer.WriteAsync(@event, cancellationToken);
        
        // Write to unified channel (for strictly ordered delivery)
        await _unifiedChannel.Writer.WriteAsync(@event!, cancellationToken);
    }

    public IAsyncEnumerable<T> SubscribeAsync<T>(CancellationToken cancellationToken = default)
    {
        var channel = GetChannel<T>();
        return channel.Reader.ReadAllAsync(cancellationToken);
    }

    public IAsyncEnumerable<object> SubscribeAllAsync(CancellationToken cancellationToken = default)
    {
        return _unifiedChannel.Reader.ReadAllAsync(cancellationToken);
    }

    private Channel<T> GetChannel<T>()
    {
        return (Channel<T>)_channels.GetOrAdd(typeof(T), _ => Channel.CreateUnbounded<T>(new UnboundedChannelOptions 
        { 
            SingleReader = false, 
            SingleWriter = false 
        }));
    }
}
