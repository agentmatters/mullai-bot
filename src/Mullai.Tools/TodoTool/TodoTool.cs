using System.ComponentModel;
using Microsoft.Extensions.AI;
using Mullai.Tools.TodoTool.Models;

namespace Mullai.Tools.TodoTool;

/// <summary>
///     A tool for managing a simple TODO list.
/// </summary>
[Description("A tool for managing tasks and TODOs during a project.")]
public class TodoTool(TodoProvider todoProvider)
{
    /// <summary>
    ///     Reads the current TODO list.
    /// </summary>
    [Description("Returns the current list of tasks and their status.")]
    public async Task<List<TodoItem>> ReadTodos()
    {
        return await todoProvider.GetTodosAsync();
    }

    /// <summary>
    ///     Adds a new task to the TODO list.
    /// </summary>
    [Description("Adds a new task to the project's TODO list.")]
    public async Task<string> AddTodo(
        [Description("The text of the task to add.")]
        string text)
    {
        return await todoProvider.AddTodoAsync(text);
    }

    /// <summary>
    ///     Marks a task as completed.
    /// </summary>
    [Description("Marks a specific task as completed using its ID.")]
    public async Task<string> CompleteTodo(
        [Description("The unique ID of the task to complete.")]
        string id)
    {
        return await todoProvider.CompleteTodoAsync(id);
    }

    /// <summary>
    ///     Returns the functions provided by this plugin.
    /// </summary>
    public IEnumerable<AITool> AsAITools()
    {
        yield return AIFunctionFactory.Create(ReadTodos);
        yield return AIFunctionFactory.Create(AddTodo);
        yield return AIFunctionFactory.Create(CompleteTodo);
    }
}