using Mullai.Web.Wasm.Client.Pages;
using Mullai.Web.Wasm.Components;
using Microsoft.Extensions.AI;
using Mullai.Agents;
using Mullai.Global.ServiceConfiguration;
using Mullai.Tools.WeatherTool;
using Mullai.Tools.CliTool;
using Mullai.Tools.FileSystemTool;
using Mullai.Memory;
using Mullai.Skills;
using Mullai.Providers.LLMProviders.OllamaOpenAI;
using Mullai.Providers.LLMProviders.OpenRouter;
using Mullai.Providers.LLMProviders.Gemini;
using Mullai.Providers.LLMProviders.Groq;
using Mullai.Providers.LLMProviders.Cerebras;
using Mullai.Web.Wasm.Hubs;
using Mullai.Web.Wasm.Messaging;
using Mullai.Execution.Clients;
using Mullai.Abstractions.Clients;
using Mullai.Web.Wasm.Services;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddSignalR();

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .Build();

builder.Services.ConfigureMullaiServices(config);
builder.Services.AddHostedService<EventBusForwarder>();
builder.Services.AddScoped<IMullaiClient, MullaiClient>();
builder.Services.AddScoped<IWebChatOrchestrator, WebChatOrchestrator>();
builder.Services.AddScoped<WebConfigController>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Mullai.Web.Wasm.Client._Imports).Assembly);
app.MapHub<FabricHub>("/hubs/fabric");

app.Run();
