using Microsoft.Extensions.AI;

namespace Mullai.Abstractions.Configuration;

public interface IMullaiChatClient : IChatClient
{
    string ActiveLabel { get; }
    void UpdateClients(IReadOnlyList<(string Label, IChatClient Client)> newClients);
}