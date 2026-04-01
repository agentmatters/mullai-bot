using Mullai.Abstractions.Observability;
using Mullai.TaskRuntime.Models;

namespace Mullai.TaskRuntime.Abstractions;

public interface IMullaiToolCallFeed
{
    long Publish(ToolCallObservation observation);
    IReadOnlyCollection<ToolCallFeedItem> ReadSince(long lastSequence, int take = 50);
}