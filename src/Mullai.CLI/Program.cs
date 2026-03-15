using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mullai.Global.ServiceConfiguration;
using Mullai.CLI;

namespace Mullai.CLI;

class Program
{
    static async Task Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        var serviceProvider = ServiceConfiguration.ConfigureMullaiServices(config);

        var app = new MullaiSpectreApp(serviceProvider);
        await app.RunAsync();
    }
}
