using Mullai.Abstractions.WorkflowState;
using Mullai.Global.ServiceConfiguration;
using Mullai.TaskRuntime.Abstractions;
using Mullai.TaskRuntime.Clients;
using Mullai.TaskRuntime.Options;
using Mullai.TaskRuntime.Services.Background;
using Mullai.TaskRuntime.Services.Core;
using Mullai.TaskRuntime.Services.Messaging;
using Mullai.TaskRuntime.Services.Storage.InMemory;
using Mullai.TaskRuntime.Services.Storage.Sqlite;
using Mullai.TaskRuntime.Services.WorkflowOutputHandlers;
using Mullai.Workflows;
using Mullai.Workflows.Abstractions;

namespace Mullai.TaskRuntime;

public static class MullaiTaskRuntimeServiceCollectionExtensions
{
    public static IServiceCollection AddMullaiTaskRuntime(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MullaiTaskRuntimeOptions>(configuration.GetSection(MullaiTaskRuntimeOptions.SectionName));
        services.Configure<MullaiRecurringTaskOptions>(
            configuration.GetSection(MullaiRecurringTaskOptions.SectionName));

        services.ConfigureMullaiServices(configuration);
        services.AddMullaiWorkflows();

        services.AddSingleton<IMullaiTaskQueue, InMemoryMullaiTaskQueue>();
        services.AddSingleton<IMullaiTaskStatusStore, SqliteMullaiTaskStatusStore>();
        services.AddSingleton<IMullaiToolCallFeed, InMemoryMullaiToolCallFeed>();
        services.AddSingleton<IMullaiTaskResponseChannel, MullaiTaskResponseChannel>();
        services.AddSingleton<IMullaiTaskClientFactory, WebMullaiClientFactory>();
        services.AddSingleton<IMullaiTaskExecutor, MullaiTaskExecutor>();
        services.AddSingleton<IWorkflowRunEventStore, SqliteWorkflowRunEventStore>();
        services.AddSingleton<IWorkflowOutputFailureStore, SqliteWorkflowOutputFailureStore>();
        services.AddSingleton<IWorkflowStateStore, SqliteWorkflowStateStore>();
        services.AddSingleton<IWorkflowOutputHandler, LogWorkflowOutputHandler>();
        services.AddSingleton<IWorkflowOutputHandler, WorkflowChainOutputHandler>();
        services.AddSingleton<IWorkflowOutputHandler, WebhookWorkflowOutputHandler>();

        services.AddHostedService<MullaiTaskWorkerService>();
        services.AddHostedService<WorkflowRegistryWatcherService>();
        services.AddHostedService<WorkflowTriggerSchedulerService>();
        services.AddHostedService<CronTaskSchedulerService>();

        return services;
    }
}