using System.Text;
using Mullai.TaskRuntime.Abstractions;
using Mullai.TaskRuntime.Models;

namespace Mullai.TaskRuntime.Services.Core;

public class MullaiTaskExecutor : IMullaiTaskExecutor
{
    private readonly IMullaiTaskClientFactory _clientFactory;

    public MullaiTaskExecutor(IMullaiTaskClientFactory clientFactory)
    {
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
    }

    public async Task<string> ExecuteAsync(
        MullaiTaskWorkItem workItem,
        Func<string, Task>? onResponseFragment = null,
        CancellationToken cancellationToken = default)
    {
        var client = _clientFactory.GetClient(workItem.SessionKey, workItem.AgentName);
        var responseAccumulator = new StringBuilder();

        string? provider = null;
        string? model = null;
        workItem.Metadata?.TryGetValue("provider", out provider);
        workItem.Metadata?.TryGetValue("model", out model);

        await foreach (var chunk in client.RunStreamingAsync(workItem.Prompt, provider, model, cancellationToken))
        {
            if (string.IsNullOrEmpty(chunk))
            {
                continue;
            }

            responseAccumulator.Append(chunk);

            if (onResponseFragment is not null)
            {
                await onResponseFragment(responseAccumulator.ToString()).ConfigureAwait(false);
            }
        }

        return responseAccumulator.ToString();
    }
}
