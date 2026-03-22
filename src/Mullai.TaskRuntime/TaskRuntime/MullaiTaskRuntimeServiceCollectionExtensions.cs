using Mullai.Global.ServiceConfiguration;
using Mullai.TaskRuntime.Abstractions;
using Mullai.TaskRuntime.Clients;
using Mullai.TaskRuntime.Options;
using Mullai.TaskRuntime.Services;

namespace Mullai.TaskRuntime;

public static class MullaiTaskRuntimeServiceCollectionExtensions
{
    public static IServiceCollection AddMullaiTaskRuntime(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MullaiTaskRuntimeOptions>(configuration.GetSection(MullaiTaskRuntimeOptions.SectionName));
        services.Configure<MullaiRecurringTaskOptions>(configuration.GetSection(MullaiRecurringTaskOptions.SectionName));

        services.ConfigureMullaiServices(configuration);

        services.AddSingleton<IMullaiTaskQueue, InMemoryMullaiTaskQueue>();
        services.AddSingleton<IMullaiTaskStatusStore, InMemoryMullaiTaskStatusStore>();
        services.AddSingleton<IMullaiToolCallFeed, InMemoryMullaiToolCallFeed>();
        services.AddSingleton<IMullaiTaskClientFactory, WebMullaiClientFactory>();
        services.AddSingleton<IMullaiTaskExecutor, MullaiTaskExecutor>();

        services.AddHostedService<MullaiTaskWorkerService>();
        services.AddHostedService<CronTaskSchedulerService>();

        return services;
    }
}
