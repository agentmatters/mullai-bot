using Mullai.TaskRuntime.Abstractions;
using Mullai.TaskRuntime.Models;

namespace Mullai.TaskRuntime.Services;

public class MullaiTaskExecutor : IMullaiTaskExecutor
{
    private readonly IMullaiTaskClientFactory _clientFactory;

    public MullaiTaskExecutor(IMullaiTaskClientFactory clientFactory)
    {
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
    }

    public async Task<string> ExecuteAsync(MullaiTaskWorkItem workItem, CancellationToken cancellationToken = default)
    {
        var client = _clientFactory.GetClient(workItem.SessionKey, workItem.AgentName);
        return await client.RunAsync(workItem.Prompt, cancellationToken).ConfigureAwait(false);
    }
}
