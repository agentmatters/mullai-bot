using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mullai.Agents;
using Mullai.Logging.LLMRequestLogging;
using Mullai.Tools.WeatherTool;
using Mullai.Memory;
using Mullai.Middleware.Middlewares;
using Mullai.OpenTelemetry.OpenTelemetry;
using Mullai.Providers;
using Mullai.Abstractions.Configuration;
using Mullai.Skills;
using Mullai.Tools.CliTool;
using Mullai.Tools.FileSystemTool;
using Mullai.Tools.WordTool;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using Mullai.Abstractions.Orchestration;
using Mullai.Orchestration;
using Mullai.Abstractions.Messaging;
using Mullai.Abstractions.Persistence;
using Mullai.Abstractions.Execution;
using Mullai.Execution.Messaging;
using Mullai.Execution.Persistence;
using Mullai.Execution.Scheduling;
using Mullai.Execution.Workers;
using Mullai.Execution.Actors;

using Mullai.Abstractions.Clients;
using Mullai.Execution.Clients;

namespace Mullai.Global.ServiceConfiguration
{
    public static class ServiceConfiguration
    {
        public static IServiceProvider ConfigureMullaiServices(IConfiguration configuration)
        {
            OpenTelemetrySettings.Initialize(configuration);
            
            var serviceCollection = new ServiceCollection();

            serviceCollection.ConfigureMullaiServices(configuration);

            return serviceCollection.BuildServiceProvider();
        }

        public static IServiceCollection ConfigureMullaiServices(this IServiceCollection services, IConfiguration configuration)
        {
            OpenTelemetrySettings.Initialize(configuration);
            
            services
                .AddSingleton<IConfiguration>(configuration)
                .AddLogging(builder =>
                {
#if DEBUG
                    builder
                        .AddConsole()
                        .SetMinimumLevel(LogLevel.Information)
                        .AddOpenTelemetry(options =>
                        {
                            options.SetResourceBuilder(
                                ResourceBuilder.CreateDefault().AddService(
                                    OpenTelemetrySettings.ServiceName, 
                                    serviceVersion: OpenTelemetrySettings.ServiceVersion));
                            options.AddOtlpExporter(
                                otlpOptions => otlpOptions.Endpoint = new Uri(OpenTelemetrySettings.OtlpEndpoint));
                            options.IncludeScopes = true;
                            options.IncludeFormattedMessage = true;
                        });
#else
                    builder.SetMinimumLevel(LogLevel.None);
#endif
                })
                .AddSingleton<LLMRequestLoggingHandler>()
                .AddSingleton<HttpClient>(sp => {
                    var loggingHandler = sp.GetService<LLMRequestLoggingHandler>();
                    loggingHandler!.InnerHandler = new HttpClientHandler();
                    return new HttpClient(loggingHandler);
                })
                .AddSingleton<IMullaiConfigurationManager, MullaiConfigurationManager>()
                .AddSingleton<ICredentialStorage>(sp => sp.GetRequiredService<IMullaiConfigurationManager>())
                .AddSingleton<IChatClient>(sp => 
                {
                    var httpClient = sp.GetRequiredService<HttpClient>();
                    var logger = sp.GetRequiredService<ILogger<MullaiChatClient>>();
                    var configManager = sp.GetRequiredService<IMullaiConfigurationManager>();

                    return MullaiChatClientFactory.Create(configuration, configManager, httpClient, logger);
                })
                .AddSingleton<AgentFactory>()
                .AddSingleton<IConversationManager, ConversationManager>()
                .AddSingleton<IPlanner, Planner>()
                .AddSingleton<IAgentRouter, AgentRouter>()
                .AddSingleton<IWorkflowEngine, WorkflowEngine>()
                .AddSingleton<IEventBus, InternalEventBus>()
                .AddSingleton<IStateStore, SqliteStateStore>()
                .AddSingleton<IScheduler, MullaiScheduler>()
                .AddSingleton<IActorManager, ActorManager>()
                .AddSingleton<IMullaiClient, MullaiClient>()
                .AddHostedService<AgentWorker>()
                .AddSingleton<FunctionCallingMiddleware>()
                .AddWeatherTool()
                .AddCliTool()
                .AddFileSystemTool()
                .AddWordTool()
                .AddUserMemory()
                .AddMullaiSkills();
            
            return services;
        }
    }
}
