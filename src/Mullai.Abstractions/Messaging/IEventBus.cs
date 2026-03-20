using System.Collections.Generic;
using System.Threading.Channels;
using Mullai.Abstractions.Observability;
using Mullai.Abstractions.Orchestration;

namespace Mullai.Abstractions.Messaging;

public interface IEventBus
{
    ValueTask PublishAsync<T>(T @event, CancellationToken cancellationToken = default);
    IAsyncEnumerable<T> SubscribeAsync<T>(CancellationToken cancellationToken = default);
    IAsyncEnumerable<object> SubscribeAllAsync(CancellationToken cancellationToken = default);
}

// public record TaskStatusEvent(string TaskId, string Status, string? Message = null);
public record AgentUpdateEvent(string AgentName, string Content);

/// <summary> Event fired when a task changes its lifecycle state (InProgress, Completed, Failed). </summary>
public record TaskStatusEvent(string TaskId, string TraceId, string Status, string? Message = null);

/// <summary> Event fired for every "word" (token) an agent generates to simulate streaming. </summary>
public record TokenReceivedEvent(string TaskId, string Token, string AgentName = "");

/// <summary> Event fired when a tool call is observed. </summary>
public record ToolCallEvent(ToolCallObservation Observation);

/// <summary> Event indicating Human-in-the-Loop (HITL) manual intervention is needed. </summary>
public record ApprovalRequestedEvent(string TaskId, string Description);

/// <summary> Used for auditing and generating the final execution report. </summary>
public record TraceUpdateEvent(string TraceId, string TaskId, string Agent, string Detail);

/// <summary> Economics: Tracks simulated resource consumption per request. </summary>
public record CostUpdateEvent(string TraceId, double Cost);

/// <summary> Event fired when the planner has generated a task graph. </summary>
public record GraphCreatedEvent(TaskGraph Graph);

