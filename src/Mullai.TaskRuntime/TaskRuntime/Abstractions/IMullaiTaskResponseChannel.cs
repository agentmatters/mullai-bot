using System.Threading.Channels;
using Mullai.TaskRuntime.Models;

namespace Mullai.TaskRuntime.Abstractions;

public interface IMullaiTaskResponseChannel
{
    ChannelReader<TaskResponseFeedItem> Reader { get; }
    ChannelWriter<TaskResponseFeedItem> Writer { get; }
}