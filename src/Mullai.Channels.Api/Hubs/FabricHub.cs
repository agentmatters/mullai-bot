using Microsoft.AspNetCore.SignalR;
using Mullai.Abstractions.Orchestration;

namespace Mullai.Channels.Api.Hubs;

public class FabricHub : Hub
{
    private readonly IConversationManager _conversationManager;

    public FabricHub(IConversationManager conversationManager)
    {
        _conversationManager = conversationManager;
    }

    public async Task JoinSession(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
    }

    public async Task<List<Microsoft.Extensions.AI.ChatMessage>> GetHistory(string sessionId)
    {
        var history = new List<Microsoft.Extensions.AI.ChatMessage>();
        await foreach (var msg in _conversationManager.GetHistoryAsync(sessionId))
        {
            history.Add(msg);
        }
        return history;
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
