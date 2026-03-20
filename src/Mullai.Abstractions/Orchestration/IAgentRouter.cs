using Mullai.Abstractions.Agents;

namespace Mullai.Abstractions.Orchestration;

public interface IAgentRouter
{
    Task<IMullaiAgent> RouteAsync(string taskDescription, CancellationToken cancellationToken = default);
}
