using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Mullai.Abstractions;
using Mullai.Abstractions.Configuration;

namespace Mullai.Agents;

public class MullaiAgent
{
    private readonly AIAgent _agent;
    private readonly IChatClient _client;

    public MullaiAgent(AIAgent agent, IChatClient client)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public IChatClient ChatClient => _client;

    public string Name => _agent.Name;
    
    public string ProviderName => (_client as IMullaiChatClient)?.ActiveLabel?.Split('/')[0] ?? "Unknown";
    public string ModelName => (_client as IMullaiChatClient)?.ActiveLabel?.Split('/').ElementAtOrDefault(1) ?? "Unknown";

    public async Task<AgentSession> CreateSessionAsync(CancellationToken cancellationToken = default) 
        => await _agent.CreateSessionAsync(cancellationToken);

    public IAsyncEnumerable<object> RunStreamingAsync(string userInput, AgentSession session, string? provider = null, string? model = null, CancellationToken cancellationToken = default)
    {
        if (provider != null || model != null)
        {
            MullaiRequestContext.Current = new MullaiRequestInfo { Provider = provider, Model = model };
        }

        try
        {
            return _agent.RunStreamingAsync(userInput, session, cancellationToken: cancellationToken);
        }
        finally
        {
            // Note: We don't clear MullaiRequestContext.Current here because the 
            // IAsyncEnumerable might still be being iterated. AsyncLocal 
            // will naturally be scoped to the execution flow.
        }
    }

    public async Task<object> RunAsync(string userInput, AgentSession session, string? provider = null, string? model = null, CancellationToken cancellationToken = default)
    {
        if (provider != null || model != null)
        {
            MullaiRequestContext.Current = new MullaiRequestInfo { Provider = provider, Model = model };
        }

        try
        {
            return await _agent.RunAsync(userInput, session, cancellationToken: cancellationToken);
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
}
