using System.Threading.Channels;
using Mullai.TaskRuntime.Abstractions;
using Mullai.TaskRuntime.Models;

namespace Mullai.TaskRuntime.Services.Messaging;

public class MullaiTaskResponseChannel : IMullaiTaskResponseChannel
{
    private readonly Channel<TaskResponseFeedItem> _channel =
        Channel.CreateUnbounded<TaskResponseFeedItem>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });

    public ChannelReader<TaskResponseFeedItem> Reader => _channel.Reader;
    public ChannelWriter<TaskResponseFeedItem> Writer => _channel.Writer;
}