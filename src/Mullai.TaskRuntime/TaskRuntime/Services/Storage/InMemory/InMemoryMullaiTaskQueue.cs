using System.Threading.Channels;
using Mullai.TaskRuntime.Abstractions;
using Mullai.TaskRuntime.Models;
using Mullai.TaskRuntime.Options;

namespace Mullai.TaskRuntime.Services.Storage.InMemory;

public class InMemoryMullaiTaskQueue : IMullaiTaskQueue
{
    private readonly Channel<MullaiTaskWorkItem> _channel;

    public InMemoryMullaiTaskQueue(IOptions<MullaiTaskRuntimeOptions> options)
    {
        var capacity = Math.Max(10, options.Value.QueueCapacity);
        _channel = Channel.CreateBounded<MullaiTaskWorkItem>(
            new BoundedChannelOptions(capacity)
            {
                SingleReader = false,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait
            });
    }

    public ValueTask EnqueueAsync(MullaiTaskWorkItem workItem, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(workItem, cancellationToken);
    }

    public ValueTask<MullaiTaskWorkItem> DequeueAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }
}