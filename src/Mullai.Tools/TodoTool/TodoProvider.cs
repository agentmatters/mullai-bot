using System.Text.Json;
using Mullai.Tools.TodoTool.Models;

namespace Mullai.Tools.TodoTool;

public class TodoProvider
{
    private static readonly string TodoFilePath = Path.Combine(Environment.CurrentDirectory, "todo.json");

    public async Task<List<TodoItem>> GetTodosAsync()
    {
        try
        {
            if (!File.Exists(TodoFilePath)) return new List<TodoItem>();

            var content = await File.ReadAllTextAsync(TodoFilePath);
            return JsonSerializer.Deserialize<List<TodoItem>>(content) ?? new List<TodoItem>();
        }
        catch (Exception)
        {
            return new List<TodoItem>();
        }
    }

    public async Task<string> UpdateTodosAsync(List<TodoItem> todos)
    {
        try
        {
            var content = JsonSerializer.Serialize(todos, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(TodoFilePath, content);
            return $"Successfully updated {todos.Count} todos.";
        }
        catch (Exception ex)
        {
            return $"Failed to update todos. Error: {ex.Message}";
        }
    }

    public async Task<string> AddTodoAsync(string text)
    {
        var todos = await GetTodosAsync();
        todos.Add(new TodoItem { Text = text, Status = "pending", CreatedAt = DateTime.UtcNow });
        return await UpdateTodosAsync(todos);
    }

    public async Task<string> CompleteTodoAsync(string id)
    {
        var todos = await GetTodosAsync();
        var todo = todos.FirstOrDefault(t => t.Id == id);
        if (todo != null)
        {
            todo.Status = "completed";
            return await UpdateTodosAsync(todos);
        }

        return $"Error: Todo with ID '{id}' not found.";
    }
}