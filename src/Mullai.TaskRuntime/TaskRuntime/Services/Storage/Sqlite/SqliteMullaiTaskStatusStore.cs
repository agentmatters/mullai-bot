using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mullai.Abstractions.Models;
using Mullai.TaskRuntime.Abstractions;
using Mullai.TaskRuntime.Models;
using Mullai.TaskRuntime.Options;

namespace Mullai.TaskRuntime.Services.Storage.Sqlite;

public sealed class SqliteMullaiTaskStatusStore : IMullaiTaskStatusStore
{
    private const string InitSql = """
        CREATE TABLE IF NOT EXISTS task_status (
            task_id TEXT PRIMARY KEY,
            session_key TEXT NOT NULL,
            agent_name TEXT NOT NULL,
            source TEXT NOT NULL,
            workflow_id TEXT,
            state TEXT NOT NULL,
            attempt INTEGER NOT NULL,
            max_attempts INTEGER NOT NULL,
            response TEXT,
            error TEXT,
            input_tokens INTEGER NOT NULL DEFAULT 0,
            output_tokens INTEGER NOT NULL DEFAULT 0,
            total_tokens INTEGER NOT NULL DEFAULT 0,
            updated_at_utc TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS idx_task_status_updated ON task_status(updated_at_utc DESC);
        CREATE INDEX IF NOT EXISTS idx_task_status_workflow ON task_status(workflow_id);
        """;

    private readonly IWorkflowRunEventStore _runEventStore;
    private readonly int _retentionCount;

    public SqliteMullaiTaskStatusStore(
        IOptions<MullaiTaskRuntimeOptions> runtimeOptions,
        IWorkflowRunEventStore runEventStore)
    {
        _retentionCount = Math.Max(0, runtimeOptions?.Value.WorkflowRunRetentionCount ?? 0);
        _runEventStore = runEventStore ?? throw new ArgumentNullException(nameof(runEventStore));
        using var connection = SqliteStoreHelper.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = InitSql;
        command.ExecuteNonQuery();

        // Migration: check if columns exist
        using var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = "PRAGMA table_info(task_status)";
        using var reader = checkCommand.ExecuteReader();
        var hasInputTokens = false;
        while (reader.Read())
        {
            if (reader.GetString(1) == "input_tokens") hasInputTokens = true;
        }
        if (!hasInputTokens)
        {
            using var migrateCommand = connection.CreateCommand();
            migrateCommand.CommandText = """
                ALTER TABLE task_status ADD COLUMN input_tokens INTEGER NOT NULL DEFAULT 0;
                ALTER TABLE task_status ADD COLUMN output_tokens INTEGER NOT NULL DEFAULT 0;
                ALTER TABLE task_status ADD COLUMN total_tokens INTEGER NOT NULL DEFAULT 0;
                """;
            migrateCommand.ExecuteNonQuery();
        }
    }

    public Task MarkQueuedAsync(MullaiTaskWorkItem workItem, CancellationToken cancellationToken = default)
        => UpsertAsync(workItem, MullaiTaskState.Queued, null, null, null, cancellationToken);

    public Task MarkRunningAsync(MullaiTaskWorkItem workItem, string? response = null, MullaiUsage? usage = null, CancellationToken cancellationToken = default)
        => UpsertAsync(workItem, MullaiTaskState.Running, response, null, usage, cancellationToken);

    public Task MarkRetryScheduledAsync(MullaiTaskWorkItem workItem, string error, CancellationToken cancellationToken = default)
        => UpsertAsync(workItem, MullaiTaskState.RetryScheduled, null, error, null, cancellationToken);

    public Task MarkSucceededAsync(MullaiTaskWorkItem workItem, string response, MullaiUsage? usage = null, CancellationToken cancellationToken = default)
        => UpsertAsync(workItem, MullaiTaskState.Succeeded, response, null, usage, cancellationToken);

    public Task MarkFailedAsync(MullaiTaskWorkItem workItem, string error, CancellationToken cancellationToken = default)
        => UpsertAsync(workItem, MullaiTaskState.Failed, null, error, null, cancellationToken);

    public Task<MullaiTaskStatusSnapshot?> GetAsync(string taskId, CancellationToken cancellationToken = default)
    {
        using var connection = SqliteStoreHelper.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM task_status WHERE task_id = $taskId";
        command.Parameters.AddWithValue("$taskId", taskId);
        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return Task.FromResult<MullaiTaskStatusSnapshot?>(null);
        }

        return Task.FromResult<MullaiTaskStatusSnapshot?>(ReadSnapshot(reader));
    }

    public Task<IReadOnlyCollection<MullaiTaskStatusSnapshot>> GetRecentAsync(int take = 50, CancellationToken cancellationToken = default)
    {
        using var connection = SqliteStoreHelper.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM task_status ORDER BY updated_at_utc DESC LIMIT $take";
        command.Parameters.AddWithValue("$take", Math.Max(1, take));
        using var reader = command.ExecuteReader();

        var results = new List<MullaiTaskStatusSnapshot>();
        while (reader.Read())
        {
            results.Add(ReadSnapshot(reader));
        }

        return Task.FromResult<IReadOnlyCollection<MullaiTaskStatusSnapshot>>(results);
    }

    public Task<MullaiUsage> GetTotalUsageAsync(string sessionKey, CancellationToken cancellationToken = default)
    {
        using var connection = SqliteStoreHelper.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT SUM(input_tokens), SUM(output_tokens), SUM(total_tokens) FROM task_status WHERE session_key = $sessionKey";
        command.Parameters.AddWithValue("$sessionKey", sessionKey);
        using var reader = command.ExecuteReader();
        if (!reader.Read() || reader.IsDBNull(0))
        {
            return Task.FromResult(new MullaiUsage(0, 0, 0));
        }

        return Task.FromResult(new MullaiUsage(
            reader.GetInt64(0),
            reader.GetInt64(1),
            reader.GetInt64(2)));
    }

    private static MullaiTaskStatusSnapshot ReadSnapshot(SqliteDataReader reader)
    {
        return new MullaiTaskStatusSnapshot
        {
            TaskId = reader.GetString(reader.GetOrdinal("task_id")),
            SessionKey = reader.GetString(reader.GetOrdinal("session_key")),
            AgentName = reader.GetString(reader.GetOrdinal("agent_name")),
            Source = Enum.TryParse<MullaiTaskSource>(reader.GetString(reader.GetOrdinal("source")), out var source)
                ? source
                : MullaiTaskSource.Client,
            WorkflowId = reader.IsDBNull(reader.GetOrdinal("workflow_id"))
                ? null
                : reader.GetString(reader.GetOrdinal("workflow_id")),
            State = Enum.TryParse<MullaiTaskState>(reader.GetString(reader.GetOrdinal("state")), out var state)
                ? state
                : MullaiTaskState.Queued,
            Attempt = reader.GetInt32(reader.GetOrdinal("attempt")),
            MaxAttempts = reader.GetInt32(reader.GetOrdinal("max_attempts")),
            Response = reader.IsDBNull(reader.GetOrdinal("response"))
                ? null
                : reader.GetString(reader.GetOrdinal("response")),
            Error = reader.IsDBNull(reader.GetOrdinal("error"))
                ? null
                : reader.GetString(reader.GetOrdinal("error")),
            InputTokenCount = reader.GetInt64(reader.GetOrdinal("input_tokens")),
            OutputTokenCount = reader.GetInt64(reader.GetOrdinal("output_tokens")),
            TotalTokenCount = reader.GetInt64(reader.GetOrdinal("total_tokens")),
            UpdatedAtUtc = DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("updated_at_utc")))
        };
    }

    private async Task UpsertAsync(
        MullaiTaskWorkItem workItem,
        MullaiTaskState state,
        string? response,
        string? error,
        MullaiUsage? usage,
        CancellationToken cancellationToken)
    {
        string? workflowId = null;
        workItem.Metadata?.TryGetValue("workflowId", out workflowId);
        using var connection = SqliteStoreHelper.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO task_status (
                task_id,
                session_key,
                agent_name,
                source,
                workflow_id,
                state,
                attempt,
                max_attempts,
                response,
                error,
                input_tokens,
                output_tokens,
                total_tokens,
                updated_at_utc
            ) VALUES (
                $taskId,
                $sessionKey,
                $agentName,
                $source,
                $workflowId,
                $state,
                $attempt,
                $maxAttempts,
                $response,
                $error,
                $inputTokens,
                $outputTokens,
                $totalTokens,
                $updatedAtUtc
            )
            ON CONFLICT(task_id) DO UPDATE SET
                session_key = excluded.session_key,
                agent_name = excluded.agent_name,
                source = excluded.source,
                workflow_id = excluded.workflow_id,
                state = excluded.state,
                attempt = excluded.attempt,
                max_attempts = excluded.max_attempts,
                response = excluded.response,
                error = excluded.error,
                input_tokens = excluded.input_tokens,
                output_tokens = excluded.output_tokens,
                total_tokens = excluded.total_tokens,
                updated_at_utc = excluded.updated_at_utc;
            """;
        command.Parameters.AddWithValue("$taskId", workItem.TaskId);
        command.Parameters.AddWithValue("$sessionKey", workItem.SessionKey);
        command.Parameters.AddWithValue("$agentName", workItem.AgentName);
        command.Parameters.AddWithValue("$source", workItem.Source.ToString());
        command.Parameters.AddWithValue("$workflowId", (object?)workflowId ?? DBNull.Value);
        command.Parameters.AddWithValue("$state", state.ToString());
        command.Parameters.AddWithValue("$attempt", workItem.Attempt);
        command.Parameters.AddWithValue("$maxAttempts", workItem.MaxAttempts);
        command.Parameters.AddWithValue("$response", (object?)response ?? DBNull.Value);
        command.Parameters.AddWithValue("$error", (object?)error ?? DBNull.Value);
        command.Parameters.AddWithValue("$inputTokens", usage?.InputTokenCount ?? 0);
        command.Parameters.AddWithValue("$outputTokens", usage?.OutputTokenCount ?? 0);
        command.Parameters.AddWithValue("$totalTokens", usage?.TotalTokenCount ?? 0);
        command.Parameters.AddWithValue("$updatedAtUtc", DateTimeOffset.UtcNow.ToString("O"));
        command.ExecuteNonQuery();

        if (state is MullaiTaskState.Succeeded or MullaiTaskState.Failed &&
            !string.IsNullOrWhiteSpace(workflowId))
        {
            await PruneRunsAsync(workflowId, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task PruneRunsAsync(string workflowId, CancellationToken cancellationToken)
    {
        if (_retentionCount <= 0)
        {
            return;
        }

        using var connection = SqliteStoreHelper.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT task_id
            FROM task_status
            WHERE workflow_id = $workflowId
            ORDER BY updated_at_utc DESC
            LIMIT -1 OFFSET $keep;
            """;
        command.Parameters.AddWithValue("$workflowId", workflowId);
        command.Parameters.AddWithValue("$keep", _retentionCount);

        using var reader = command.ExecuteReader();
        var taskIds = new List<string>();
        while (reader.Read())
        {
            taskIds.Add(reader.GetString(reader.GetOrdinal("task_id")));
        }

        if (taskIds.Count == 0)
        {
            return;
        }

        using var deleteCommand = connection.CreateCommand();
        var parameters = taskIds.Select((_, index) => $"$id{index}").ToArray();
        deleteCommand.CommandText = $"DELETE FROM task_status WHERE task_id IN ({string.Join(",", parameters)});";
        for (var i = 0; i < taskIds.Count; i++)
        {
            deleteCommand.Parameters.AddWithValue(parameters[i], taskIds[i]);
        }
        deleteCommand.ExecuteNonQuery();

        await _runEventStore.RemoveByTaskIdsAsync(taskIds, cancellationToken).ConfigureAwait(false);
    }
}
