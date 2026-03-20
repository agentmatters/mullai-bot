using Microsoft.AspNetCore.SignalR;
using Mullai.Abstractions.Orchestration;
using Mullai.Web.Wasm.Messaging;

namespace Mullai.Web.Wasm.Hubs;

public class FabricHub : Hub
{
    private readonly IWebChatOrchestrator _chatOrchestrator;

    public FabricHub(IWebChatOrchestrator chatOrchestrator)
    {
        _chatOrchestrator = chatOrchestrator;
    }

    public async Task JoinSession(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        try
        {
            await _chatOrchestrator.InitializeAsync(sessionId, Context.ConnectionAborted);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("OnSystemAlert", "error", ex.Message);
        }
    }

    public async Task LeaveSession(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
    }

    public async Task SendMessage(string sessionId, string input, string? mode)
    {
        input = input?.Trim() ?? string.Empty;
        if (!Enum.TryParse<ExecutionMode>(mode, true, out var parsedMode))
        {
            parsedMode = ExecutionMode.Team;
        }

        try
        {
            await _chatOrchestrator.SendMessageAsync(sessionId, input, parsedMode, Context.ConnectionAborted);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("OnSystemAlert", "error", ex.Message);
        }
    }
}

public interface IFabricClient
{
    Task OnTaskUpdate(string nodeId, string status, string? message);
    Task OnAgentToken(string taskId, string agentName, string token);
    Task OnSystemAlert(string level, string message);
}
