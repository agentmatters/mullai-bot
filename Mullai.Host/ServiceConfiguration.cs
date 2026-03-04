using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mullai.Host.Logging;
using Mullai.Providers.LLMProviders.OpenRouter;
using Mullai.Tools.WeatherTool;

namespace Mullai.Host
{
    public static class ServiceConfiguration
    {
        public static IServiceProvider ConfigureServices()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            
            var serviceCollection = new ServiceCollection();

            serviceCollection
                .AddSingleton<IConfiguration>(config)
                .AddLogging(builder =>
                {
                    builder
                        .AddConsole()
                        .SetMinimumLevel(LogLevel.None);
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
                    return OpenRouter.GetOpenRouterChatClient(config, loggerFactory, httpClient);
                })
                .AddWeatherTool();

            return serviceCollection.BuildServiceProvider();
        }
    }
}
