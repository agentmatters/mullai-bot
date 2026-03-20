namespace Mullai.Web.Wasm.Messaging;

public class ToolCallSnapshot
{
    public string ToolName { get; set; } = "";
    public string? Result { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset FinishedAt { get; set; }
    public string TaskId { get; set; } = "";
    public string AgentName { get; set; } = "";
}
