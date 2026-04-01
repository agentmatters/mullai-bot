using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Mullai.Providers.Common.Http;

/// <summary>
///     A base class for HTTP-based IChatClient implementations.
///     Handles authentication, request/response cycle, and SSE streaming.
/// </summary>
public abstract class BaseHttpChatClient<TRequest, TResponse, TStream> : IChatClient
{
    protected readonly IProviderAdapter<TRequest, TResponse, TStream> Adapter;
    protected readonly IMessageConsolidator? Consolidator;
    protected readonly Uri EndpointUri;
    protected readonly HttpClient HttpClient;

    protected BaseHttpChatClient(
        HttpClient httpClient,
        Uri endpointUri,
        IProviderAdapter<TRequest, TResponse, TStream> adapter,
        IMessageConsolidator? consolidator = null)
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        EndpointUri = endpointUri ?? throw new ArgumentNullException(nameof(endpointUri));
        Adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        Consolidator = consolidator;
    }

    public Action<HttpRequestMessage>? OnBeforeRequest { get; set; }

    public virtual ChatClientMetadata Metadata => new(GetType().Name, EndpointUri);

    public virtual async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var processedMessages = Consolidator?.Consolidate(messages) ?? messages;
        var requestDto = Adapter.MapRequest(processedMessages, options, false);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, EndpointUri)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestDto), Encoding.UTF8, "application/json")
        };

        OnBeforeRequest?.Invoke(httpRequest);

        using var response = await HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException(
                $"Response status code does not indicate success: {(int)response.StatusCode} ({response.StatusCode}).\nContent: {errorContent}",
                null, response.StatusCode);
        }

        var responseDto = await JsonSerializer.DeserializeAsync<TResponse>(
            await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (responseDto == null)
            throw new InvalidOperationException("Failed to deserialize provider response.");

        return Adapter.MapResponse(responseDto);
    }

    public virtual async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var processedMessages = Consolidator?.Consolidate(messages) ?? messages;
        var requestDto = Adapter.MapRequest(processedMessages, options, true);

        using var request = new HttpRequestMessage(HttpMethod.Post, EndpointUri)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestDto), Encoding.UTF8, "application/json")
        };

        OnBeforeRequest?.Invoke(request);

        using var response = await HttpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException(
                $"Response status code does not indicate success: {(int)response.StatusCode} ({response.StatusCode}).\nContent: {errorContent}",
                null, response.StatusCode);
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (line == null) break;
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6).Trim();
                if (data == "[DONE]") break;

                var updateDto = JsonSerializer.Deserialize<TStream>(data);
                if (updateDto != null) yield return Adapter.MapStreamingUpdate(updateDto);
            }
        }
    }

    public virtual object? GetService(Type serviceType, object? key = null)
    {
        if (serviceType == typeof(ChatClientMetadata)) return Metadata;
        return serviceType.IsInstanceOfType(this) ? this : null;
    }

    public virtual void Dispose()
    {
        // HttpClient is typically managed by a factory, but we check if we should dispose it.
        // For now, we follow the pattern of not disposing injected clients.
    }
}