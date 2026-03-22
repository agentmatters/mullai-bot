using System.Collections.Concurrent;
using Mullai.Abstractions.Observability;
using Mullai.TaskRuntime.Abstractions;
using Mullai.TaskRuntime.Execution;
using Mullai.TaskRuntime.Models;

namespace Mullai.TaskRuntime.Services;

public class InMemoryMullaiToolCallFeed : IMullaiToolCallFeed
{
    private readonly ConcurrentQueue<ToolCallFeedItem> _events = new();
    private long _sequence;
    private const int MaxItems = 4000;

    public long Publish(ToolCallObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);

        var sequence = Interlocked.Increment(ref _sequence);
        var context = MullaiTaskExecutionContext.Current;
        _events.Enqueue(new ToolCallFeedItem
        {
            Sequence = sequence,
            TaskId = context?.TaskId,
            SessionKey = context?.SessionKey,
            Observation = observation
        });

        while (_events.Count > MaxItems && _events.TryDequeue(out _))
        {
            // Trim old events and keep a bounded in-memory feed.
        }

        return sequence;
    }

    public IReadOnlyCollection<ToolCallFeedItem> ReadSince(long lastSequence, int take = 50)
    {
        var resolvedTake = Math.Max(1, take);
        return _events
            .Where(x => x.Sequence > lastSequence)
            .OrderBy(x => x.Sequence)
            .Take(resolvedTake)
            .ToArray();
    }
}
