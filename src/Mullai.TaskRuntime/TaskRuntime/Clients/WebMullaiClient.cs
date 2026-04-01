using Mullai.Agents;
using Mullai.Agents.Clients;

namespace Mullai.TaskRuntime.Clients;

public class WebMullaiClient : BaseMullaiClient
{
    public WebMullaiClient(
        AgentFactory agentFactory,
        string agentName) : base(agentFactory, agentName)
    {
    }
}