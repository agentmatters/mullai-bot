namespace Mullai.Abstractions.Clients;

public interface IMullaiClient
{
    string ProviderName { get; }
    string ModelName { get; }

    Task InitialiseAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> RunStreamingAsync(string userInput, CancellationToken cancellationToken = default);
    Task<string> RunAsync(string userInput, CancellationToken cancellationToken = default);
    void RefreshClients();
}
