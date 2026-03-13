using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mullai.Global.ServiceConfiguration;
using Mullai.OpenTelemetry.OpenTelemetry;
using Mullai.TUI.TUI.App;

namespace Mullai.TUI;

class Program
{
    static async Task Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var serviceProvider = ServiceConfiguration.ConfigureMullaiServices(config);

        using var tracer = OpenTelemetryProvider.SetupTracerProvider(config);
        using var meter = OpenTelemetryProvider.SetupMeterProvider(config);

        // Hand off to the Terminal.Gui TUI — replaces the plain console loop
        await MullaiApp.Launch(serviceProvider);
    }
}
