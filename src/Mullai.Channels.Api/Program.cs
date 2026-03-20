using Mullai.Channels.Api;
using Mullai.Channels.Core;
using Microsoft.AspNetCore.Mvc;
using Mullai.Channels.Api.Hubs;
using Mullai.Channels.Api.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddSignalR();

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    // .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

builder.Services.AddMullaiAgentServices(config);
builder.Services.AddHostedService<EventBusForwarder>();

var app = builder.Build();

// Instantiate ChannelManager to wire up adapter events before services start
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<ChannelManager>();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Generic Webhook endpoint for processing incoming multi-channel messages
app.MapPost("/api/webhooks/{channelId}", async (string channelId, [FromBody] object payload, ChannelManager channelManager) =>
{
    var adapter = channelManager.GetAdapter(channelId);
    if (adapter == null)
    {
        return Results.NotFound($"Channel adapter not found for '{channelId}'");
    }

    try
    {
        await adapter.ProcessIncomingMessageAsync(payload);
        return Results.Ok();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error processing webhook for channel {ChannelId}", channelId);
        return Results.Problem("Error processing webhook.");
    }
})
.WithName("ProcessChannelWebhook");

app.MapHub<FabricHub>("/hubs/fabric");

app.Run();
