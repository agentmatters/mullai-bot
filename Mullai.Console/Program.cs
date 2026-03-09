using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mullai.Agents;
using Mullai.Global.ServiceConfiguration;
using Mullai.OpenTelemetry.OpenTelemetry;

namespace Mullai.Console
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
            
            var serviceProvider = ServiceConfiguration.ConfigureMullaiServices(config);
            
            using var tracer = OpenTelemetryProvider.SetupTracerProvider(config);
            using var meter = OpenTelemetryProvider.SetupMeterProvider(config);

            var agentFactory = serviceProvider.GetRequiredService<AgentFactory>();
            var agent = agentFactory.GetAgent("Assistant");
            
            // Create a persistent session for multi-turn conversation
            var session = await agent.CreateSessionAsync();
            
            System.Console.WriteLine("Mullai Chat");
            System.Console.WriteLine("Type your message and press Enter. Type 'exit' to quit.");

            while (true)
            {
                System.Console.Write("You: ");
                var userInput = System.Console.ReadLine();

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
                                System.Console.Write("\r" + new string(' ', 50) + "\r");
                                System.Console.Write("Agent: ");
                                firstUpdate = false;
                            }
                            System.Console.Write(update);
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
                        System.Console.Write("\r" + new string(' ', 50) + "\r");
                        System.Console.Write($"Agent: {response}");
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
                
                System.Console.WriteLine("\n");
            }

            System.Console.WriteLine("Goodbye!");
        }

        static async Task ShowThinkingAsync(CancellationToken ct)
        {
            var dots = new[] { ".", "..", "..." };
            int dotIndex = 0;

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    System.Console.Write($"\rAgent: Thinking{dots[dotIndex++ % dots.Length]}   ");
                    await Task.Delay(250, ct);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
