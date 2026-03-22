using Mullai.Web.Components;
using Mullai.Middleware.Middlewares;
using Mullai.TaskRuntime;
using Mullai.TaskRuntime.Abstractions;
using Mullai.TaskRuntime.Execution;
using Mullai.TaskRuntime.Models;
using System.Text.Json;
using Mullai.Abstractions.Configuration;
using Mullai.Global.ServiceConfiguration;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});
builder.WebHost.UseUrls("http://0.0.0.0:7755");
builder.Host.UseWindowsService();
builder.Host.UseSystemd();

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
var runEventStore = app.Services.GetRequiredService<IWorkflowRunEventStore>();
var statusStore = app.Services.GetRequiredService<IMullaiTaskStatusStore>();
functionCallingMiddleware.OnToolCallObserved = observation =>
{
    toolCallFeed.Publish(observation);

    var context = MullaiTaskExecutionContext.Current;
    if (context is null)
    {
        return;
    }

    var status = statusStore.GetAsync(context.TaskId).GetAwaiter().GetResult();
    var workflowId = status?.WorkflowId;
    if (string.IsNullOrWhiteSpace(workflowId))
    {
        return;
    }

    var eventType = observation.IsFinished ? "tool_call_finished" : "tool_call_started";
    var createdAt = observation.FinishedAt ?? observation.StartedAt;
    string payloadJson;
    try
    {
        payloadJson = JsonSerializer.Serialize(observation);
    }
    catch
    {
        payloadJson = JsonSerializer.Serialize(new
        {
            observation.CallId,
            observation.ToolName,
            observation.Succeeded,
            observation.Result,
            observation.Error
        });
    }

    var runEvent = new WorkflowRunEvent
    {
        TaskId = context.TaskId,
        WorkflowId = workflowId,
        EventType = eventType,
        PayloadJson = payloadJson,
        CreatedAtUtc = createdAt
    };

    runEventStore.AppendAsync(runEvent).GetAwaiter().GetResult();
};

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
