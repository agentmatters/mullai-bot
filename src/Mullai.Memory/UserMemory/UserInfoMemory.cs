using System.Text;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Mullai.Memory.UserMemory;

/// <summary>
///     Sample memory component that can remember a user's name and age.
///     This component subscribes to all messages added to the conversation and
///     extracts the user's name and age if provided.
///     The component adds a prompt to ask for this information if it is not already known
///     and provides it to the model before each invocation if known.
/// </summary>
public sealed class UserInfoMemory : AIContextProvider
{
    private readonly IChatClient _chatClient;
    private readonly ProviderSessionState<UserInfo> _sessionState;
    private IReadOnlyList<string>? _stateKeys;

    public UserInfoMemory(IChatClient chatClient, Func<AgentSession?, UserInfo>? stateInitializer = null)
    {
        _sessionState = new ProviderSessionState<UserInfo>(
            stateInitializer ?? (_ => new UserInfo()),
            GetType().Name);
        _chatClient = chatClient;
    }

    public IReadOnlyList<string> StateKeys => _stateKeys ??= [_sessionState.StateKey];

    public UserInfo GetUserInfo(AgentSession session)
    {
        return _sessionState.GetOrInitializeState(session);
    }

    public void SetUserInfo(AgentSession session, UserInfo userInfo)
    {
        _sessionState.SaveState(session, userInfo);
    }

    protected override async ValueTask StoreAIContextAsync(InvokedContext context,
        CancellationToken cancellationToken = default)
    {
        var userInfo = _sessionState.GetOrInitializeState(context.Session);

        // Try and extract the user name and age from the message if we don't have it already and it's a user message.
        if ((userInfo.UserName is null || userInfo.UserAge is null) &&
            context.RequestMessages.Any(x => x.Role == ChatRole.User))
        {
            var result = await _chatClient.GetResponseAsync<UserInfo>(
                context.RequestMessages,
                new ChatOptions
                {
                    Instructions =
                        "Extract the user's name and age from the message if present. If not present return nulls."
                },
                cancellationToken: cancellationToken);

            userInfo.UserName ??= result.Result.UserName;
            userInfo.UserAge ??= result.Result.UserAge;
        }

        _sessionState.SaveState(context.Session, userInfo);
    }

    protected override ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context,
        CancellationToken cancellationToken = default)
    {
        var userInfo = _sessionState.GetOrInitializeState(context.Session);

        StringBuilder instructions = new();

        // If we don't already know the user's name and age, add instructions to ask for them, otherwise just provide what we have to the context.
        instructions
            .AppendLine(
                userInfo.UserName is null
                    ? "Ask the user for their name and politely decline to answer any questions until they provide it."
                    : $"The user's name is {userInfo.UserName}.")
            .AppendLine(
                userInfo.UserAge is null
                    ? "Ask the user for their age and politely decline to answer any questions until they provide it."
                    : $"The user's age is {userInfo.UserAge}.");

        return new ValueTask<AIContext>(new AIContext
        {
            Instructions = instructions.ToString()
        });
    }
}