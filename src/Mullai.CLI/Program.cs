using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mullai.Global.ServiceConfiguration;
using Mullai.CLI;

namespace Mullai.CLI;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            var serviceProvider = ServiceConfiguration.ConfigureMullaiServices(config);

            var app = new MullaiSpectreApp(serviceProvider);
            await app.RunAsync();
        }
        catch (Exception ex)
        {
#if DEBUG
            throw;
#else
            Spectre.Console.AnsiConsole.MarkupLine($"[red]Error:[/] {Spectre.Console.Markup.Escape(ex.Message)}");
            Environment.Exit(1);
#endif
        }
    }
}
