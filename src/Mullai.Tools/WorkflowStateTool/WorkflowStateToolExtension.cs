using Microsoft.Extensions.DependencyInjection;

namespace Mullai.Tools.WorkflowStateTool;

public static class WorkflowStateToolExtension
{
    public static IServiceCollection AddWorkflowStateTool(this IServiceCollection services)
    {
        services.AddSingleton<WorkflowStateTool>();
        return services;
    }
}
