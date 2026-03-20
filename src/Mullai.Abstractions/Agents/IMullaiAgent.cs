using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;

namespace Mullai.Abstractions.Agents;

public interface IMullaiAgent
{
    public string Name {get; set;}
    public string Instructions { get; set; }
    IAsyncEnumerable<AgentResponseUpdate> RunStreamingAsync(string userInput, AgentSession session, CancellationToken cancellationToken = default);
    Task<AgentResponse> RunAsync(string userInput, AgentSession session, CancellationToken cancellationToken = default);
}