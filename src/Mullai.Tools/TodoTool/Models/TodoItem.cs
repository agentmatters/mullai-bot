namespace Mullai.Tools.TodoTool.Models;

public class TodoItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 4);
    public string Text { get; set; } = string.Empty;
    public string Status { get; set; } = "pending"; // pending, completed
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}