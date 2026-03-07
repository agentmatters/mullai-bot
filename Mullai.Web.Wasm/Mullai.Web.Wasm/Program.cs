using Mullai.Web.Wasm.Client.Pages;
using Mullai.Web.Wasm.Components;
using Microsoft.Extensions.AI;
using Mullai.Agents;
using Mullai.Tools.WeatherTool;
using Mullai.Tools.CliTool;
using Mullai.Memory;
using Mullai.Skills;
using Mullai.Providers.LLMProviders.OllamaOpenAI;
using Mullai.Providers.LLMProviders.OpenRouter;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddSingleton<HttpClient>();

builder.Services.AddSingleton<IChatClient>(sp => 
{
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    var httpClient = sp.GetRequiredService<HttpClient>();
    var config = sp.GetRequiredService<IConfiguration>();

    return OpenRouter.GetOpenRouterChatClient(config, loggerFactory, httpClient);
    // return OllamaOpenAI.GetOllamaOpenAIChatClient(config, loggerFactory, httpClient);
});

builder.Services.AddSingleton<AgentFactory>();
builder.Services.AddWeatherTool()
    .AddCliTool()
    .AddUserMemory()
    .AddMullaiSkills();


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

app.Run();