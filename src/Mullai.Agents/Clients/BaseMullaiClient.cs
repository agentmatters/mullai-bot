using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Mullai.Abstractions.Clients;

namespace Mullai.Agents.Clients;

public abstract class BaseMullaiClient : IMullaiClient
{
    private readonly AgentFactory _agentFactory;
    private readonly string _agentName;
    private readonly SemaphoreSlim _initialisationLock = new(1, 1);
    private readonly SemaphoreSlim _executionLock = new(1, 1);
    private MullaiAgent? _agent;
    private AgentSession? _session;

    protected BaseMullaiClient(
        AgentFactory agentFactory,
        string agentName = "Assistant")
    {
        _agentFactory = agentFactory ?? throw new ArgumentNullException(nameof(agentFactory));
        _agentName = string.IsNullOrWhiteSpace(agentName) ? "Assistant" : agentName;
    }

    public string ProviderName => _agent?.ProviderName ?? "Unknown";
    public string ModelName => _agent?.ModelName ?? "Unknown";

    public async Task InitialiseAsync(CancellationToken cancellationToken = default)
    {
        if (_agent is not null && _session is not null)
        {
            return;
        }

        await _initialisationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_agent is not null && _session is not null)
            {
                return;
            }

            _agent = _agentFactory.GetAgent(_agentName);
            _session = await _agent.CreateSessionAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _initialisationLock.Release();
        }
    }

    public async IAsyncEnumerable<string> RunStreamingAsync(
        string userInput,
        string? provider = null,
        string? model = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await InitialiseAsync(cancellationToken).ConfigureAwait(false);
        await _executionLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var agent = _agent!;
            var session = _session!;

            await foreach (var update in agent.RunStreamingAsync(userInput, session, provider, model, cancellationToken))
            {
                var text = update?.ToString();
                if (!string.IsNullOrEmpty(text))
                {
                    yield return text;
                }
            }
        }
        finally
        {
            _executionLock.Release();
        }
    }

    public async Task<string> RunAsync(string userInput, string? provider = null, string? model = null, CancellationToken cancellationToken = default)
    {
        await InitialiseAsync(cancellationToken).ConfigureAwait(false);
        await _executionLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var response = await _agent!.RunAsync(userInput, _session!, provider, model, cancellationToken).ConfigureAwait(false);
            return response?.ToString() ?? string.Empty;
        }
        finally
        {
            _executionLock.Release();
        }
    }

    public virtual void RefreshClients()
    {
    }

    protected void RefreshAgentClients(Action<MullaiAgent> refreshAction)
    {
        if (_agent is null || refreshAction is null)
        {
            return;
        }

        _agent.RefreshClients(() => refreshAction(_agent));
    }
}
