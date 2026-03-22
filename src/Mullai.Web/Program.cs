using Mullai.Web.Components;
using Mullai.Middleware.Middlewares;
using Mullai.TaskRuntime;
using Mullai.TaskRuntime.Abstractions;
using Mullai.Abstractions.Configuration;
using Mullai.Global.ServiceConfiguration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMullaiTaskRuntime(builder.Configuration);
builder.Services.AddSingleton<HttpClient>();
builder.Services.AddSingleton<IMullaiConfigurationManager, MullaiConfigurationManager>();

var app = builder.Build();

// Wire tool-call observations from middleware into the web tool-call feed.
var functionCallingMiddleware = app.Services.GetRequiredService<FunctionCallingMiddleware>();
var toolCallFeed = app.Services.GetRequiredService<IMullaiToolCallFeed>();
functionCallingMiddleware.OnToolCallObserved = observation => toolCallFeed.Publish(observation);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
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
    .AddInteractiveServerRenderMode();
app.MapMullaiTaskEndpoints();

app.Run();
