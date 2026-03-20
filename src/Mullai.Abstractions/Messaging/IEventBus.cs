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

public interface ISessionEvent
{
    string SessionId { get; }
}

public record AgentUpdateEvent(string SessionId, string AgentName, string Content) : ISessionEvent;

/// <summary> Event fired when a task changes its lifecycle state (InProgress, Completed, Failed). </summary>
public record TaskStatusEvent(string TaskId, string SessionId, string Status, string? Message = null) : ISessionEvent;

/// <summary> Event fired for every "word" (token) an agent generates to simulate streaming. </summary>
public record TokenReceivedEvent(string TaskId, string SessionId, string Token, string AgentName = "") : ISessionEvent;

/// <summary> Event fired when a tool call is observed. </summary>
public record ToolCallEvent(string SessionId, ToolCallObservation Observation) : ISessionEvent;

/// <summary> Event indicating Human-in-the-Loop (HITL) manual intervention is needed. </summary>
public record ApprovalRequestedEvent(string TaskId, string SessionId, string Description) : ISessionEvent;

/// <summary> Used for auditing and generating the final execution report. </summary>
public record TraceUpdateEvent(string SessionId, string TaskId, string Agent, string Detail) : ISessionEvent;

/// <summary> Economics: Tracks simulated resource consumption per request. </summary>
public record CostUpdateEvent(string SessionId, double Cost) : ISessionEvent;

/// <summary> Event fired when the planner has generated a task graph. </summary>
public record GraphCreatedEvent(string SessionId, TaskGraph Graph) : ISessionEvent;

