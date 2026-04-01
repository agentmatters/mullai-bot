using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mullai.Abstractions.Clients;
using Mullai.CLI.Clients;
using Mullai.CLI.Components;
using Mullai.CLI.Controllers;
using Mullai.CLI.State;
using Mullai.Global.ServiceConfiguration;
using Mullai.Middleware.Middlewares;
using RazorConsole.Core;

var hostBuilder = Host.CreateDefaultBuilder(args)
    .UseRazorConsole<App>();

hostBuilder.ConfigureAppConfiguration((context, config) =>
{
    config.SetBasePath(AppContext.BaseDirectory);
    config.AddJsonFile("appsettings.json", true, true);
});

hostBuilder.ConfigureServices((context, services) =>
{
    services.ConfigureMullaiServices(context.Configuration);

    services.AddSingleton<ChatState>();
    services.AddSingleton<IMullaiClient, CliMullaiClient>();
    services.AddSingleton<ChatOrchestrator>();
    services.AddSingleton<ConfigController>();

    services.Configure<ConsoleAppOptions>(options =>
    {
        options.AutoClearConsole = false;
        options.EnableTerminalResizing = true;
    });
});

var host = hostBuilder.Build();

// Wire tool call observations into the singleton channel
var middleware = host.Services.GetRequiredService<FunctionCallingMiddleware>();
middleware.OnToolCallObserved = obs => ToolCallChannel.Instance.Writer.TryWrite(obs);

await host.RunAsync();