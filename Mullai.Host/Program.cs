using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mullai.Agents;
using Mullai.Global.Config.OpenTelemetry;
using Mullai.Host.Telemetry;

namespace Mullai.Host
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Initialize the configuration and build service provider
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            
            //Initialize OpenTelemetrySettings with Config values
            OpenTelemetrySettings.Initialize(config);
            
            var serviceProvider = ServiceConfiguration.ConfigureServices(config);
            
            using var tracer = OpenTelemetryProvider.SetupTracerProvider(config);
            using var meter = OpenTelemetryProvider.SetupMeterProvider(config);

            var agentFactory = new AgentFactory(serviceProvider);
            var agent = agentFactory.GetAgent("Assistant");
            
            // Create a persistent session for multi-turn conversation
            var session = await agent.CreateSessionAsync();
            
            Console.WriteLine("Mullai Chat");
            Console.WriteLine("Type your message and press Enter. Type 'exit' to quit.");

            while (true)
            {
                Console.Write("You: ");
                var userInput = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(userInput))
                    continue;

                if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    break;
                
                // Use CancellationTokenSource to control the thinking animation
                using var cts = new CancellationTokenSource();
                var thinkingTask = ShowThinkingAsync(cts.Token);
                
                try
                {
                    bool enableStreaming = config.GetValue<bool>("EnableStreaming", true);
                    
                    if (enableStreaming)
                    {
                        var firstUpdate = true;
                        // Stream the response from the agent
                        await foreach (var update in agent.RunStreamingAsync(userInput, session))
                        {
                            // Cancel thinking animation on first update
                            if (firstUpdate)
                            {
                                await cts.CancelAsync();
                                try
                                {
                                    await thinkingTask;
                                }
                                catch
                                {
                                    // ignored
                                }

                                // Clear the "Thinking..." line
                                Console.Write("\r" + new string(' ', 50) + "\r");
                                Console.Write("Agent: ");
                                firstUpdate = false;
                            }
                            Console.Write(update);
                        }
                    }
                    else
                    {
                        // Invoke the agent and output the text result.
                        var response = await agent.RunAsync(userInput, session);
                        await cts.CancelAsync();
                        try
                        {
                            await thinkingTask;
                        }
                        catch
                        {
                            // ignored
                        }

                        // Clear the "Thinking..." line
                        Console.Write("\r" + new string(' ', 50) + "\r");
                        Console.Write($"Agent: {response}");
                    }
                }
                finally
                {
                    await cts.CancelAsync();
                    try { await thinkingTask; }
                    catch
                    {
                        // ignored
                    }
                }
                
                Console.WriteLine("\n");
            }

            Console.WriteLine("Goodbye!");
        }

        static async Task ShowThinkingAsync(CancellationToken ct)
        {
            var dots = new[] { ".", "..", "..." };
            int dotIndex = 0;

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    Console.Write($"\rAgent: Thinking{dots[dotIndex++ % dots.Length]}   ");
                    await Task.Delay(250, ct);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
