using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Mullai.Abstractions;
using Mullai.Abstractions.Configuration;
using Mullai.Abstractions.Models;

namespace Mullai.Agents;

public class MullaiAgent
{
    private readonly AIAgent _agent;

    public MullaiAgent(AIAgent agent, IChatClient client)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        ChatClient = client ?? throw new ArgumentNullException(nameof(client));
    }

    public IChatClient ChatClient { get; }

    public string Name => _agent.Name;

    public string ProviderName =>
        MullaiRequestContext.Current?.Provider ??
        (ChatClient as IMullaiChatClient)?.ActiveLabel?.Split('/')[0] ??
        "Unknown";

    public string ModelName =>
        MullaiRequestContext.Current?.Model ??
        (ChatClient as IMullaiChatClient)?.ActiveLabel?.Split('/').ElementAtOrDefault(1) ??
        "Unknown";

    public async Task<AgentSession> CreateSessionAsync(CancellationToken cancellationToken = default)
    {
        return await _agent.CreateSessionAsync(cancellationToken);
    }

    public async IAsyncEnumerable<object> RunStreamingAsync(
        string userInput,
        AgentSession session,
        string? provider = null,
        string? model = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (provider != null || model != null)
            MullaiRequestContext.Current = new MullaiRequestInfo { Provider = provider, Model = model };

        List<AgentResponseUpdate> updates = [];
        try
        {
            await foreach (var update in _agent.RunStreamingAsync(userInput, session,
                               cancellationToken: cancellationToken))
            {
                updates.Add(update);
                yield return update;
            }
        }
        finally
        {
            // Reset context after enumeration if we set it
            if (provider != null || model != null) MullaiRequestContext.Current = null;
        }

        if (updates.Any())
        {
            var collectedResponseFromStreaming = updates.ToAgentResponse();
            if (collectedResponseFromStreaming.Usage != null)
            {
                var usage = new MullaiUsage(
                    collectedResponseFromStreaming.Usage.InputTokenCount ?? 0,
                    collectedResponseFromStreaming.Usage.OutputTokenCount ?? 0,
                    collectedResponseFromStreaming.Usage.TotalTokenCount ?? 0);
                yield return usage;
            }
        }
    }

    public async Task<object> RunAsync(string userInput, AgentSession session, string? provider = null,
        string? model = null, CancellationToken cancellationToken = default)
    {
        if (provider != null || model != null)
            MullaiRequestContext.Current = new MullaiRequestInfo { Provider = provider, Model = model };

        try
        {
            var response = await _agent.RunAsync(userInput, session, cancellationToken: cancellationToken);
            return response;
        }
        finally
        {
            MullaiRequestContext.Current = null;
        }
    }

    public void RefreshClients(Action refreshAction)
    {
        refreshAction?.Invoke();
    }

    public AIFunction AsAIFunction()
    {
        return _agent.AsAIFunction();
    }
}