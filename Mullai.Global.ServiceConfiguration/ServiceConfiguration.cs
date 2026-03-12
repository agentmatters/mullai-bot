using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mullai.Agents;
using Mullai.Logging.LLMRequestLogging;
using Mullai.Logging.TuiLogging;
using Mullai.Tools.WeatherTool;
using Mullai.Memory;
using Mullai.Middleware.Middlewares;
using Mullai.OpenTelemetry.OpenTelemetry;
using Mullai.Providers;
using Mullai.Skills;
using Mullai.Tools.CliTool;
using Mullai.Tools.FileSystemTool;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

namespace Mullai.Global.ServiceConfiguration
{
    public static class ServiceConfiguration
    {
    public static IServiceProvider ConfigureMullaiServices(IConfiguration configuration, bool useConsoleLogging = false)
    {
        OpenTelemetrySettings.Initialize(configuration);
        
        var serviceCollection = new ServiceCollection();

        serviceCollection.ConfigureMullaiServices(configuration, useConsoleLogging);

        return serviceCollection.BuildServiceProvider();
    }

    public static IServiceCollection ConfigureMullaiServices(this IServiceCollection services, IConfiguration configuration, bool useConsoleLogging = false)
    {
        OpenTelemetrySettings.Initialize(configuration);
        
        services
            .AddSingleton<IConfiguration>(configuration)
            .AddLogging(builder =>
            {
                if (useConsoleLogging)
                {
                    // Use standard console logging
                    builder.AddConsole();
                }
                else
                {
                    // Use TUI logger provider (no console output, logs appear in right panel)
                    builder.AddProvider(new TuiLoggerProvider(TuiLogLevel.Trace));
                }

                builder
                    .SetMinimumLevel(LogLevel.Trace)
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
            })
                .AddSingleton<LLMRequestLoggingHandler>()
                .AddSingleton<HttpClient>(sp => {
                    var loggingHandler = sp.GetService<LLMRequestLoggingHandler>();
                    loggingHandler!.InnerHandler = new HttpClientHandler();
                    return new HttpClient(loggingHandler);
                })
                .AddSingleton<IChatClient>(sp => 
                {
                    var httpClient = sp.GetRequiredService<HttpClient>();
                    var logger = sp.GetRequiredService<ILogger<MullaiChatClient>>();

                    var modelsJsonPath = Path.Combine(
                        AppContext.BaseDirectory, "models.json");

                    return MullaiChatClientFactory.Create(modelsJsonPath, configuration, httpClient, logger);
                })
                .AddSingleton<AgentFactory>()
                .AddSingleton<FunctionCallingMiddleware>()
                .AddWeatherTool()
                .AddCliTool()
                .AddFileSystemTool()
                .AddUserMemory()
                .AddMullaiSkills();
            
            return services;
        }
    }
}
