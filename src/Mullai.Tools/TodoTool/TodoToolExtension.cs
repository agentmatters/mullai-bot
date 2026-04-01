using Microsoft.Extensions.DependencyInjection;

namespace Mullai.Tools.TodoTool;

public static class TodoToolExtension
{
    public static IServiceCollection AddTodoTool(
        this IServiceCollection services)
    {
        services.AddSingleton<TodoProvider>();
        services.AddSingleton<TodoTool>();

        return services;
    }
}