using Microsoft.Extensions.DependencyInjection;
using Mullai.Workflows.Abstractions;
using Mullai.Workflows.Services;

namespace Mullai.Workflows;

public static class WorkflowServiceCollectionExtensions
{
    public static IServiceCollection AddMullaiWorkflows(this IServiceCollection services)
    {
        services.AddSingleton<FileSystemWorkflowRegistry>();
        services.AddSingleton<IWorkflowRegistry>(sp => sp.GetRequiredService<FileSystemWorkflowRegistry>());
        services.AddSingleton<IWorkflowRegistryReloader>(sp => sp.GetRequiredService<FileSystemWorkflowRegistry>());
        services.AddSingleton<IWorkflowFactory, WorkflowFactory>();
        services.AddSingleton<IWorkflowAgentFactory, WorkflowAgentFactory>();
        services.AddSingleton<IWorkflowOutputDispatcher, WorkflowOutputDispatcher>();
        return services;
    }
}