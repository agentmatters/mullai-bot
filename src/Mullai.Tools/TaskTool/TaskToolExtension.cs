using Microsoft.Extensions.DependencyInjection;

namespace Mullai.Tools.TaskTool;

public static class TaskToolExtension
{
    public static IServiceCollection AddTaskTool(this IServiceCollection services)
    {
        services.AddScoped<TaskTool>();
        return services;
    }
}
