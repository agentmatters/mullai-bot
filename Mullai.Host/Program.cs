using Mullai.Agents;

namespace Mullai.Host
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Initialize the configuration and build service provider
            var serviceProvider = ServiceConfiguration.ConfigureServices();

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
