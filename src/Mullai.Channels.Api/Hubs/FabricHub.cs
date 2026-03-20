using Microsoft.AspNetCore.SignalR;

namespace Mullai.Channels.Api.Hubs;

public class FabricHub : Hub
{
    public async Task JoinSession(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
    }

    public async Task LeaveSession(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
    }
}

public interface IFabricClient
{
    Task OnTaskUpdate(string nodeId, string status, string? message);
    Task OnAgentToken(string agentName, string token);
    Task OnSystemAlert(string level, string message);
}
