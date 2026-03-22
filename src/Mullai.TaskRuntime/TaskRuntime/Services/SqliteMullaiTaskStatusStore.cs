using System.Text.Json;
using Microsoft.Data.Sqlite;
using Mullai.TaskRuntime.Abstractions;
using Mullai.TaskRuntime.Models;

namespace Mullai.TaskRuntime.Services;

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
            updated_at_utc TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS idx_task_status_updated ON task_status(updated_at_utc DESC);
        CREATE INDEX IF NOT EXISTS idx_task_status_workflow ON task_status(workflow_id);
        """;

    public SqliteMullaiTaskStatusStore()
    {
        using var connection = SqliteStoreHelper.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = InitSql;
        command.ExecuteNonQuery();
    }

    public Task MarkQueuedAsync(MullaiTaskWorkItem workItem, CancellationToken cancellationToken = default)
    {
        Upsert(workItem, MullaiTaskState.Queued, null, null);
        return Task.CompletedTask;
    }

    public Task MarkRunningAsync(MullaiTaskWorkItem workItem, string? response = null, CancellationToken cancellationToken = default)
    {
        Upsert(workItem, MullaiTaskState.Running, response, null);
        return Task.CompletedTask;
    }

    public Task MarkRetryScheduledAsync(MullaiTaskWorkItem workItem, string error, CancellationToken cancellationToken = default)
    {
        Upsert(workItem, MullaiTaskState.RetryScheduled, null, error);
        return Task.CompletedTask;
    }

    public Task MarkSucceededAsync(MullaiTaskWorkItem workItem, string response, CancellationToken cancellationToken = default)
    {
        Upsert(workItem, MullaiTaskState.Succeeded, response, null);
        return Task.CompletedTask;
    }

    public Task MarkFailedAsync(MullaiTaskWorkItem workItem, string error, CancellationToken cancellationToken = default)
    {
        Upsert(workItem, MullaiTaskState.Failed, null, error);
        return Task.CompletedTask;
    }

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
            UpdatedAtUtc = DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("updated_at_utc")))
        };
    }

    private static void Upsert(MullaiTaskWorkItem workItem, MullaiTaskState state, string? response, string? error)
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
        command.Parameters.AddWithValue("$updatedAtUtc", DateTimeOffset.UtcNow.ToString("O"));
        command.ExecuteNonQuery();
    }
}
