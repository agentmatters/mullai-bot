using Mullai.Abstractions.Clients;
using Mullai.Abstractions.Orchestration;

namespace Mullai.Web.Wasm.Messaging;

public interface IWebChatOrchestrator
{
    Task InitializeAsync(string sessionId, CancellationToken ct = default);
    Task SendMessageAsync(string sessionId, string input, ExecutionMode mode, CancellationToken ct = default);
}

public class WebChatOrchestrator : IWebChatOrchestrator
{
    private readonly IMullaiClient _mullaiClient;

    public WebChatOrchestrator(IMullaiClient mullaiClient)
    {
        _mullaiClient = mullaiClient;
    }

    public Task InitializeAsync(string sessionId, CancellationToken ct = default)
    {
        return _mullaiClient.InitialiseAsync(sessionId, ct);
    }

    public async Task SendMessageAsync(string sessionId, string input, ExecutionMode mode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        await _mullaiClient.InitialiseAsync(sessionId, ct);
        await _mullaiClient.SendPromptAsync(input, mode, ct);
    }
}
