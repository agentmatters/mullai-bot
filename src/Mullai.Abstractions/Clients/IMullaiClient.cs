namespace Mullai.Abstractions.Clients;

public interface IMullaiClient
{
    string ProviderName { get; }
    string ModelName { get; }

    Task InitialiseAsync(CancellationToken cancellationToken = default);
    IAsyncEnumerable<object> RunStreamingAsync(string userInput, string? provider = null, string? model = null, CancellationToken cancellationToken = default);
    Task<string> RunAsync(string userInput, string? provider = null, string? model = null, CancellationToken cancellationToken = default);
    void RefreshClients();
}
