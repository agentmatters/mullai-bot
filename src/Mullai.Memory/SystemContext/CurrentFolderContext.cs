using Microsoft.Agents.AI;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mullai.Memory.SystemContext;

/// <summary>
/// A context provider that injects the host's current working directory into the agent's instructions.
/// </summary>
public sealed class CurrentFolderContext : AIContextProvider
{
    public class CurrentFolderState
    {
        public bool HasProvidedContext { get; set; } = false;
    }

    private readonly ProviderSessionState<CurrentFolderState> _sessionState;

    public CurrentFolderContext(Func<AgentSession?, CurrentFolderState>? stateInitializer = null)
    {
        _sessionState = new ProviderSessionState<CurrentFolderState>(
            stateInitializer ?? (_ => new CurrentFolderState()),
            this.GetType().Name);
    }

    protected override ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        var state = _sessionState.GetOrInitializeState(context.Session);

        if (!state.HasProvidedContext)
        {
            state.HasProvidedContext = true;
            var currentDirectory = Environment.CurrentDirectory;

            return new ValueTask<AIContext>(new AIContext
            {
                Instructions = $"{context.AIContext.Instructions} \n The agent is currently operating in the following directory: {currentDirectory}. Use this as the base path for relative operations."
            });
        }

        return new ValueTask<AIContext>(new AIContext());
    }

    protected override ValueTask StoreAIContextAsync(InvokedContext context, CancellationToken cancellationToken = default)
    {
        var state = _sessionState.GetOrInitializeState(context.Session);
        _sessionState.SaveState(context.Session, state);
        return default;
    }
}
