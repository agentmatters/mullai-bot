using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mullai.Agents;
using Mullai.Host.Logging;
using Mullai.Providers.LLMProviders.OpenRouter;
using Mullai.Tools.WeatherTool;

namespace Mullai.Host
{
    class Program
    {
        private static IConfiguration _config;
        public static IServiceProvider _serviceProvider;

        static async Task Main(string[] args)
        {
            // Initialize the configuration
            InitialiseConfig();

            var agentFactory = new AgentFactory(_serviceProvider);

            var agent = agentFactory.GetAgent("Assistant");
            
            // Invoke the agent and output the text result.
            // Console.WriteLine(await agent.RunAsync("Tell me current time and weather in Seattle."));

            // Invoke the agent with streaming support.
            await foreach (var update in agent.RunStreamingAsync("Tell me current time and weather in Seattle."))
            {
                Console.Write(update);
            }
        }

        static void InitialiseConfig()
        {
            _config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            
            var serviceCollection = new ServiceCollection();

            serviceCollection
                .AddSingleton<IConfiguration>(_config)
                .AddLogging(builder =>
                {
                    builder
                        .AddConsole()
                        .SetMinimumLevel(LogLevel.Information);
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
                    return OpenRouter.GetOpenRouterChatClient(_config, loggerFactory, httpClient);
                })
                .AddWeatherTool();

            _serviceProvider = serviceCollection.BuildServiceProvider();
        }
    }
}