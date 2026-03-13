using System.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Mullai.OpenTelemetry.OpenTelemetry;

namespace Mullai.Providers;

/// <summary>
/// An IChatClient implementation that wraps multiple ordered provider-model clients,
/// automatically falls back to the next on failure, and instruments every
/// invocation with OpenTelemetry traces and structured log events.
/// </summary>
public class MullaiChatClient : IChatClient
{
    // ── OpenTelemetry Activity source ──────────────────────────────────────
    internal static readonly ActivitySource ActivitySource =
        new(OpenTelemetrySettings.ServiceName, "1.0.0");

    private readonly IReadOnlyList<(string Label, IChatClient Client)> _clients;
    private readonly ILogger<MullaiChatClient> _logger;
    private readonly ChatClientMetadata _metadata;

    public MullaiChatClient(
        IReadOnlyList<(string Label, IChatClient Client)> clients,
        ILogger<MullaiChatClient> logger)
    {
        if (clients == null || clients.Count == 0)
            throw new ArgumentException("At least one client must be provided.", nameof(clients));

        _clients = clients;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metadata = new ChatClientMetadata("MullaiChatClient");
    }

    public ChatClientMetadata Metadata => _metadata;

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

        _logger.LogInformation(
            "MullaiChatClient starting GetResponseAsync with {ProviderCount} provider(s): [{Providers}]",
            _clients.Count,
            string.Join(", ", _clients.Select(c => c.Label)));

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
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messageList = messages as IList<ChatMessage> ?? messages.ToList();

        using var parentActivity = ActivitySource.StartActivity(
            "MullaiChatClient.GetStreamingResponse",
            ActivityKind.Client);

        parentActivity?.SetTag("mullai.client.provider_count", _clients.Count);
        parentActivity?.SetTag("mullai.streaming", true);

        _logger.LogInformation(
            "MullaiChatClient starting GetStreamingResponseAsync with {ProviderCount} provider(s): [{Providers}]",
            _clients.Count,
            string.Join(", ", _clients.Select(c => c.Label)));

        Exception? lastException = null;
        int attemptIndex = 0;

        foreach (var (label, client) in _clients)
        {
            attemptIndex++;
            var (providerName, modelId) = ParseLabel(label);

            using var attemptActivity = ActivitySource.StartActivity(
                "MullaiChatClient.StreamingAttempt",
                ActivityKind.Client);

            attemptActivity?.SetTag("mullai.provider", providerName);
            attemptActivity?.SetTag("mullai.model", modelId);
            attemptActivity?.SetTag("mullai.attempt", attemptIndex);

            _logger.LogInformation(
                "MullaiChatClient streaming attempt {Attempt}/{Total} → Provider: {Provider}, Model: {Model}",
                attemptIndex, _clients.Count, providerName, modelId);

            var buffer = new List<ChatResponseUpdate>();
            var succeeded = false;
            var sw = Stopwatch.StartNew();

            try
            {
                await foreach (var update in client.GetStreamingResponseAsync(messageList, options, cancellationToken))
                {
                    buffer.Add(update);
                }
                succeeded = true;
                sw.Stop();

                attemptActivity?.SetTag("mullai.success", true);
                attemptActivity?.SetTag("mullai.chunk_count", buffer.Count);
                attemptActivity?.SetTag("mullai.duration_ms", sw.ElapsedMilliseconds);
                parentActivity?.SetTag("mullai.winning_provider", providerName);
                parentActivity?.SetTag("mullai.winning_model", modelId);
                parentActivity?.SetTag("mullai.winning_attempt", attemptIndex);

                _logger.LogInformation(
                    "MullaiChatClient streaming succeeded on attempt {Attempt} — Provider: {Provider}, Model: {Model}, Chunks: {ChunkCount}, Duration: {DurationMs}ms",
                    attemptIndex, providerName, modelId, buffer.Count, sw.ElapsedMilliseconds);
            }
            catch (OperationCanceledException)
            {
                sw.Stop();
                _logger.LogWarning(
                    "MullaiChatClient streaming cancelled on attempt {Attempt} — Provider: {Provider}, Model: {Model}",
                    attemptIndex, providerName, modelId);

                attemptActivity?.SetStatus(ActivityStatusCode.Error, "Cancelled");
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                lastException = ex;
                buffer.Clear();

                attemptActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                attemptActivity?.SetTag("mullai.success", false);
                attemptActivity?.SetTag("mullai.error_type", ex.GetType().Name);
                attemptActivity?.SetTag("mullai.duration_ms", sw.ElapsedMilliseconds);

                _logger.LogWarning(ex,
                    "MullaiChatClient streaming attempt {Attempt}/{Total} failed — Provider: {Provider}, Model: {Model}, Error: {ErrorType}: {ErrorMessage}. Trying next.",
                    attemptIndex, _clients.Count, providerName, modelId, ex.GetType().Name, ex.Message);
            }

            if (succeeded)
            {
                foreach (var update in buffer)
                    yield return update;

                yield break;
            }
        }

        var finalError = new InvalidOperationException(
            $"All {_clients.Count} MullaiChatClient provider(s) failed during streaming. Last error: {lastException?.Message}",
            lastException);

        parentActivity?.SetStatus(ActivityStatusCode.Error, finalError.Message);
        parentActivity?.SetTag("mullai.all_failed", true);

        _logger.LogError(lastException,
            "MullaiChatClient streaming exhausted all {ProviderCount} provider(s) without success",
            _clients.Count);

        throw finalError;
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
