using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Mullai.OpenTelemetry.OpenTelemetry;
using Mullai.Abstractions.Configuration;
using Mullai.Providers.Common.Models;
using System.Text.Json;

namespace Mullai.Providers;

/// <summary>
/// An IChatClient implementation that wraps multiple ordered provider-model clients,
/// automatically falls back to the next on failure, and instruments every
/// invocation with OpenTelemetry traces and structured log events.
/// </summary>
public class MullaiChatClient : IMullaiChatClient
{
    // ── OpenTelemetry Activity source ──────────────────────────────────────
    internal static readonly ActivitySource ActivitySource =
        new(OpenTelemetrySettings.ServiceName, "1.0.0");

    private IReadOnlyList<(string Label, IChatClient Client)> _clients;
    private readonly ILogger<MullaiChatClient> _logger;
    private readonly ChatClientMetadata _metadata;
    private readonly IMullaiConfigurationManager _configManager;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public MullaiChatClient(
        IReadOnlyList<(string Label, IChatClient Client)> clients,
        ILogger<MullaiChatClient> logger,
        IMullaiConfigurationManager configManager,
        Microsoft.Extensions.Configuration.IConfiguration configuration,
        HttpClient httpClient)
    {
        _clients = clients ?? Array.Empty<(string, IChatClient)>();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _metadata = new ChatClientMetadata("MullaiChatClient");

        _configManager.OnConfigurationChanged += RefreshClients;
    }

    private void RefreshClients()
    {
        _logger.LogInformation("Configuration changed. Refreshing MullaiChatClient providers.");
        try
        {
            var config = _configManager.GetProvidersConfig();
            var customProviders = _configManager.GetCustomProviders();
            var newClients = MullaiChatClientFactory.BuildOrderedClients(
                config,
                customProviders,
                _configuration,
                _configManager,
                _httpClient);

            UpdateClients(newClients);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh MullaiChatClient providers after configuration change.");
        }
    }

    public void UpdateClients(IReadOnlyList<(string Label, IChatClient Client)> newClients)
    {
        var oldClients = _clients;
        _clients = newClients ?? Array.Empty<(string, IChatClient)>();

        // Dispose old clients to avoid memory leaks if they were replaced
        if (oldClients != null)
        {
            foreach (var (_, client) in oldClients)
            {
                try
                {
                    client.Dispose();
                }
                catch
                {
                    /* ignore */
                }
            }
        }
    }

    public ChatClientMetadata Metadata => _metadata;
    public string ActiveLabel => _clients.Count > 0 ? _clients[0].Label : "No Providers Configured";

    // ── GetResponseAsync ───────────────────────────────────────────────────
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var messageList = messages as IList<ChatMessage> ?? messages.ToList();

        using var parentActivity = ActivitySource.StartActivity(
            "MullaiChatClient.GetResponse",
            ActivityKind.Client);

        parentActivity?.SetTag("mullai.client.provider_count", _clients.Count);

        if (_clients.Count == 0)
        {
            throw new InvalidOperationException(
                "No AI providers are configured. Please use the /config command to set up at least one provider and API key.");
        }

        _logger.LogInformation(
            "MullaiChatClient starting GetResponseAsync with {ProviderCount} provider(s). Instructions: {HasInstructions}, Tools: {ToolCount}, Messages: {MessageCount}",
            _clients.Count,
            !string.IsNullOrEmpty(options?.Instructions),
            options?.Tools?.Count ?? 0,
            messageList.Count);

        Exception? lastException = null;
        int attemptIndex = 0;

        foreach (var (label, client) in _clients)
        {
            attemptIndex++;
            var (providerName, modelId) = ParseLabel(label);

            using var attemptActivity = ActivitySource.StartActivity(
                $"MullaiChatClient.Attempt",
                ActivityKind.Client);

            attemptActivity?.SetTag("mullai.provider", providerName);
            attemptActivity?.SetTag("mullai.model", modelId);
            attemptActivity?.SetTag("mullai.attempt", attemptIndex);

            _logger.LogInformation(
                "MullaiChatClient attempt {Attempt}/{Total} → Provider: {Provider}, Model: {Model}",
                attemptIndex, _clients.Count, providerName, modelId);

            var sw = Stopwatch.StartNew();
            try
            {
                var response = await client.GetResponseAsync(messageList, options, cancellationToken);
                sw.Stop();

                attemptActivity?.SetTag("mullai.success", true);
                attemptActivity?.SetTag("mullai.duration_ms", sw.ElapsedMilliseconds);
                parentActivity?.SetTag("mullai.winning_provider", providerName);
                parentActivity?.SetTag("mullai.winning_model", modelId);
                parentActivity?.SetTag("mullai.winning_attempt", attemptIndex);

                _logger.LogInformation(
                    "MullaiChatClient succeeded on attempt {Attempt} — Provider: {Provider}, Model: {Model}, Duration: {DurationMs}ms",
                    attemptIndex, providerName, modelId, sw.ElapsedMilliseconds);

                return response;
            }
            catch (OperationCanceledException)
            {
                sw.Stop();
                _logger.LogWarning(
                    "MullaiChatClient cancelled on attempt {Attempt} — Provider: {Provider}, Model: {Model}",
                    attemptIndex, providerName, modelId);

                attemptActivity?.SetStatus(ActivityStatusCode.Error, "Cancelled");
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                lastException = ex;

                attemptActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                attemptActivity?.SetTag("mullai.success", false);
                attemptActivity?.SetTag("mullai.error_type", ex.GetType().Name);
                attemptActivity?.SetTag("mullai.duration_ms", sw.ElapsedMilliseconds);

                _logger.LogWarning(ex,
                    "MullaiChatClient attempt {Attempt}/{Total} failed — Provider: {Provider}, Model: {Model}, Error: {ErrorType}: {ErrorMessage}. Trying next.",
                    attemptIndex, _clients.Count, providerName, modelId, ex.GetType().Name, ex.Message);
            }
        }

        var finalError = new InvalidOperationException(
            $"All {_clients.Count} MullaiChatClient provider(s) failed. Last error: {lastException?.Message}",
            lastException);

        parentActivity?.SetStatus(ActivityStatusCode.Error, finalError.Message);
        parentActivity?.SetTag("mullai.all_failed", true);

        _logger.LogError(lastException,
            "MullaiChatClient exhausted all {ProviderCount} provider(s) without success",
            _clients.Count);

        throw finalError;
    }

    // ── GetStreamingResponseAsync ──────────────────────────────────────────
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken = default)
    {
        var messageList = messages as IList<ChatMessage> ?? messages.ToList();

        using var parentActivity = ActivitySource.StartActivity(
            "MullaiChatClient.GetStreamingResponse",
            ActivityKind.Client);

        if (_clients.Count == 0)
            throw new InvalidOperationException("No AI providers are configured.");
        
        var (client, providerName, modelId, attemptIndex) =
            await SelectClientAsync(messageList, options, cancellationToken);

        var sw = Stopwatch.StartNew();
        var chunkCount = 0;
        
        await foreach (var update in StreamFromClient(
                           client,
                           messageList,
                           options,
                           onFirstToken: () =>
                           {
                               parentActivity?.SetTag("mullai.winning_provider", providerName);
                               parentActivity?.SetTag("mullai.winning_model", modelId);
                               parentActivity?.SetTag("mullai.winning_attempt", attemptIndex);
                           },
                           onToken: () => chunkCount++,
                           cancellationToken))
        {
            yield return update;
        }

        sw.Stop();

        parentActivity?.SetTag("mullai.chunk_count", chunkCount);
        parentActivity?.SetTag("mullai.duration_ms", sw.ElapsedMilliseconds);
    }

    private async IAsyncEnumerable<ChatResponseUpdate> StreamFromClient(
        IChatClient client,
        IList<ChatMessage> messages,
        ChatOptions? options,
        Action onFirstToken,
        Action onToken,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken)
    {
        bool first = true;

        await foreach (var update in client.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            if (first)
            {
                first = false;
                onFirstToken();
            }

            onToken();

            yield return update;
        }
    }

    private async Task<(IChatClient client, string providerName, string modelId, int attemptIndex)>
        SelectClientAsync(
            IList<ChatMessage> messages,
            ChatOptions? options,
            CancellationToken cancellationToken)
    {
        Exception? lastException = null;
        int attemptIndex = 0;

        foreach (var (label, client) in _clients)
        {
            attemptIndex++;
            var (providerName, modelId) = ParseLabel(label);

            try
            {
                await using var enumerator = client
                    .GetStreamingResponseAsync(messages, options, cancellationToken)
                    .GetAsyncEnumerator(cancellationToken);

                if (await enumerator.MoveNextAsync())
                {
                    return (client, providerName, modelId, attemptIndex);
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
            }
        }

        throw new InvalidOperationException(
            $"All providers failed before streaming started. Last error: {lastException?.Message}",
            lastException);
    }

    public object? GetService(Type serviceType, object? key = null)
    {
        if (_clients.Count > 0)
            return _clients[0].Client.GetService(serviceType, key);
        return null;
    }

    public void Dispose()
    {
        foreach (var (_, client) in _clients)
            client.Dispose();

        ActivitySource.Dispose();
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    /// <summary>Splits "ProviderName/model-id" label into its two parts.</summary>
    private static (string Provider, string Model) ParseLabel(string label)
    {
        var slash = label.IndexOf('/');
        return slash > 0
            ? (label[..slash], label[(slash + 1)..])
            : (label, string.Empty);
    }
}