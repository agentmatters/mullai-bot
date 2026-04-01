using Mullai.Tools.TodoTool;

namespace Mullai.Tools.Tests.TodoTool;

public class TodoProviderTests : IDisposable
{
    private readonly TodoProvider _provider;
    private readonly string _testFile = Path.Combine(Environment.CurrentDirectory, "todo.json");

    public TodoProviderTests()
    {
        _provider = new TodoProvider();
        if (File.Exists(_testFile)) File.Delete(_testFile);
    }

    public void Dispose()
    {
        if (File.Exists(_testFile)) File.Delete(_testFile);
    }

    [Fact]
    public async Task AddTodoAsync_AddsItemToList()
    {
        // Act
        await _provider.AddTodoAsync("Test Task");
        var todos = await _provider.GetTodosAsync();

        // Assert
        Assert.Single(todos);
        Assert.Equal("Test Task", todos[0].Text);
        Assert.Equal("pending", todos[0].Status);
    }

    [Fact]
    public async Task CompleteTodoAsync_UpdatesStatus()
    {
        // Arrange
        await _provider.AddTodoAsync("Task to complete");
        var todos = await _provider.GetTodosAsync();
        var id = todos[0].Id;

        // Act
        await _provider.CompleteTodoAsync(id);
        var updatedTodos = await _provider.GetTodosAsync();

        // Assert
        Assert.Equal("completed", updatedTodos[0].Status);
    }

    [Fact]
    public async Task CompleteTodoAsync_WithInvalidId_ReturnsError()
    {
        // Act
        var result = await _provider.CompleteTodoAsync("invalid");

        // Assert
        Assert.Contains("Error", result);
    }
}