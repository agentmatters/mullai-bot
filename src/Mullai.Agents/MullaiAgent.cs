using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Mullai.Abstractions.Agents;
using Mullai.Abstractions.Configuration;

namespace Mullai.Agents;

public class MullaiAgent : IMullaiAgent
{
    private readonly AIAgent _agent;
    private readonly IChatClient _client;

    public MullaiAgent(AIAgent agent, IChatClient client)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        Name = agent.Name;
    }

    public IChatClient ChatClient => _client;

    public string Name { get; set; }
    public string Instructions { get; set; } = string.Empty;
    
    public string ProviderName => (_client as IMullaiChatClient)?.ActiveLabel?.Split('/')[0] ?? "Unknown";
    public string ModelName => (_client as IMullaiChatClient)?.ActiveLabel?.Split('/').ElementAtOrDefault(1) ?? "Unknown";

    public async Task<AgentSession> CreateSessionAsync(CancellationToken cancellationToken = default) 
        => await _agent.CreateSessionAsync(cancellationToken);

    public IAsyncEnumerable<AgentResponseUpdate> RunStreamingAsync(string userInput, AgentSession session, CancellationToken cancellationToken = default)
    {
        return _agent.RunStreamingAsync(userInput, session, null, cancellationToken);
    }

    public async Task<AgentResponse> RunAsync(string userInput, AgentSession session, CancellationToken cancellationToken = default)
    {
        return await _agent.RunAsync(userInput, session, null, cancellationToken);
    }

    public void RefreshClients(Action refreshAction)
    {
        refreshAction?.Invoke();
    }
}
