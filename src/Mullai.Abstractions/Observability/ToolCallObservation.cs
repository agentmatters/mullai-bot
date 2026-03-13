namespace Mullai.Abstractions.Observability;

/// <summary>
/// A lightweight, UI-agnostic record of a single tool invocation event.
/// Emitted by <c>FunctionCallingMiddleware</c> via an injected callback so
/// the middleware has zero dependency on the TUI layer.
/// </summary>
public record ToolCallObservation(
    string ToolName,
    IReadOnlyDictionary<string, object?> Arguments,
    bool Succeeded,
    string? Result,
    string? Error,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt)
{
    public TimeSpan Elapsed => FinishedAt - StartedAt;
}
