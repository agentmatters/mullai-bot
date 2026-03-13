namespace Mullai.TUI.TUI.State;

/// <summary>Status of a single tool invocation.</summary>
public enum ToolCallStatus { Running, Succeeded, Failed }

/// <summary>A single captured tool call event emitted by the agent middleware.</summary>
public record ToolCallEvent(
    string ToolName,
    IReadOnlyDictionary<string, object?> Arguments,
    ToolCallStatus Status,
    string? Result = null,
    string? Error = null,
    DateTimeOffset? StartedAt = null,
    DateTimeOffset? FinishedAt = null)
{
    /// <summary>Elapsed time string, e.g. "1.3s".</summary>
    public string Elapsed =>
        (StartedAt.HasValue && FinishedAt.HasValue)
            ? $"{(FinishedAt.Value - StartedAt.Value).TotalSeconds:F1}s"
            : string.Empty;
}
