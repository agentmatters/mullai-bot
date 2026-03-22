using Mullai.Abstractions.Clients;

namespace Mullai.TaskRuntime.Abstractions;

public interface IMullaiTaskClientFactory
{
    IMullaiClient GetClient(string sessionKey, string agentName);
}
