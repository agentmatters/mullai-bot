using Microsoft.Extensions.DependencyInjection;

namespace Mullai.Tools.WorkflowTool;

public static class WorkflowToolExtension
{
    public static IServiceCollection AddWorkflowTool(this IServiceCollection services)
    {
        services.AddSingleton<WorkflowTool>();
        return services;
    }
}
