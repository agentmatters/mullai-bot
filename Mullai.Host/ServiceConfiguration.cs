using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mullai.Global.Config.OpenTelemetry;
using Mullai.Host.Logging;
using Mullai.Host.Telemetry;
using Mullai.Providers.LLMProviders.OpenRouter;
using Mullai.Providers.LLMProviders.Gemini;
using Mullai.Tools.WeatherTool;
using Mullai.Memory;
using Mullai.Skills;
using Mullai.Tools.CliTool;
using Mullai.Tools.FileSystemTool;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

namespace Mullai.Host
{
    public static class ServiceConfiguration
    {
        public static IServiceProvider ConfigureServices(IConfiguration configuration)
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection
                .AddSingleton<IConfiguration>(configuration)
                .AddLogging(builder =>
                {
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
                })
                .AddSingleton<LLMRequestLoggingHandler>()
                .AddSingleton<HttpClient>(sp => {
                    var loggingHandler = sp.GetService<LLMRequestLoggingHandler>();
                    loggingHandler!.InnerHandler = new HttpClientHandler();
                    return new HttpClient(loggingHandler);
                })
                .AddSingleton<IChatClient>(sp => 
                {
                    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                    var httpClient = sp.GetRequiredService<HttpClient>();
        
                    // Initialize your OpenRouter client using the factory
                    // return OpenRouter.GetOpenRouterChatClient(configuration, loggerFactory, httpClient);
                    return Gemini.GetGeminiChatClient(configuration, loggerFactory, httpClient);
                })
                .AddWeatherTool()
                .AddCliTool()
                .AddFileSystemTool()
                .AddUserMemory()
                .AddMullaiSkills();

            return serviceCollection.BuildServiceProvider();
        }
    }
}
