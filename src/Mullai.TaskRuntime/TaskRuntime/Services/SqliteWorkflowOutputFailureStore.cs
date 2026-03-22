using System.Text.Json;
using Microsoft.Data.Sqlite;
using Mullai.TaskRuntime.Abstractions;
using Mullai.TaskRuntime.Models;

namespace Mullai.TaskRuntime.Services;

public sealed class SqliteWorkflowOutputFailureStore : IWorkflowOutputFailureStore
{
    private const string InitSql = """
        CREATE TABLE IF NOT EXISTS workflow_output_failures (
            id TEXT PRIMARY KEY,
            workflow_id TEXT NOT NULL,
            output_type TEXT NOT NULL,
            output_target TEXT,
            output_properties TEXT,
            task_id TEXT NOT NULL,
            session_key TEXT NOT NULL,
            response TEXT NOT NULL,
            metadata TEXT,
            error TEXT NOT NULL,
            attempts INTEGER NOT NULL,
            failed_at_utc TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS idx_output_failures_failed ON workflow_output_failures(failed_at_utc DESC);
        """;

    public SqliteWorkflowOutputFailureStore()
    {
        using var connection = SqliteStoreHelper.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = InitSql;
        command.ExecuteNonQuery();
    }

    public Task AddAsync(WorkflowOutputFailure failure, CancellationToken cancellationToken = default)
    {
        using var connection = SqliteStoreHelper.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO workflow_output_failures (
                id,
                workflow_id,
                output_type,
                output_target,
                output_properties,
                task_id,
                session_key,
                response,
                metadata,
                error,
                attempts,
                failed_at_utc
            ) VALUES (
                $id,
                $workflowId,
                $outputType,
                $outputTarget,
                $outputProperties,
                $taskId,
                $sessionKey,
                $response,
                $metadata,
                $error,
                $attempts,
                $failedAtUtc
            );
            """;
        command.Parameters.AddWithValue("$id", failure.Id);
        command.Parameters.AddWithValue("$workflowId", failure.WorkflowId);
        command.Parameters.AddWithValue("$outputType", failure.OutputType);
        command.Parameters.AddWithValue("$outputTarget", (object?)failure.OutputTarget ?? DBNull.Value);
        command.Parameters.AddWithValue("$outputProperties", JsonSerializer.Serialize(failure.OutputProperties));
        command.Parameters.AddWithValue("$taskId", failure.TaskId);
        command.Parameters.AddWithValue("$sessionKey", failure.SessionKey);
        command.Parameters.AddWithValue("$response", failure.Response);
        command.Parameters.AddWithValue("$metadata", failure.Metadata is null ? DBNull.Value : JsonSerializer.Serialize(failure.Metadata));
        command.Parameters.AddWithValue("$error", failure.Error);
        command.Parameters.AddWithValue("$attempts", failure.Attempts);
        command.Parameters.AddWithValue("$failedAtUtc", failure.FailedAtUtc.ToString("O"));
        command.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public Task<WorkflowOutputFailure?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        using var connection = SqliteStoreHelper.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM workflow_output_failures WHERE id = $id";
        command.Parameters.AddWithValue("$id", id);
        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return Task.FromResult<WorkflowOutputFailure?>(null);
        }

        return Task.FromResult<WorkflowOutputFailure?>(ReadFailure(reader));
    }

    public Task<IReadOnlyCollection<WorkflowOutputFailure>> GetRecentAsync(int take = 50, CancellationToken cancellationToken = default)
    {
        using var connection = SqliteStoreHelper.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM workflow_output_failures ORDER BY failed_at_utc DESC LIMIT $take";
        command.Parameters.AddWithValue("$take", Math.Max(1, take));
        using var reader = command.ExecuteReader();

        var results = new List<WorkflowOutputFailure>();
        while (reader.Read())
        {
            results.Add(ReadFailure(reader));
        }

        return Task.FromResult<IReadOnlyCollection<WorkflowOutputFailure>>(results);
    }

    public Task RemoveAsync(string id, CancellationToken cancellationToken = default)
    {
        using var connection = SqliteStoreHelper.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM workflow_output_failures WHERE id = $id";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    private static WorkflowOutputFailure ReadFailure(SqliteDataReader reader)
    {
        var outputProperties = reader.IsDBNull(reader.GetOrdinal("output_properties"))
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(reader.GetString(reader.GetOrdinal("output_properties")))
              ?? new Dictionary<string, string>();

        var metadata = reader.IsDBNull(reader.GetOrdinal("metadata"))
            ? null
            : JsonSerializer.Deserialize<Dictionary<string, string>>(reader.GetString(reader.GetOrdinal("metadata")));

        return new WorkflowOutputFailure
        {
            Id = reader.GetString(reader.GetOrdinal("id")),
            WorkflowId = reader.GetString(reader.GetOrdinal("workflow_id")),
            OutputType = reader.GetString(reader.GetOrdinal("output_type")),
            OutputTarget = reader.IsDBNull(reader.GetOrdinal("output_target"))
                ? null
                : reader.GetString(reader.GetOrdinal("output_target")),
            OutputProperties = outputProperties,
            TaskId = reader.GetString(reader.GetOrdinal("task_id")),
            SessionKey = reader.GetString(reader.GetOrdinal("session_key")),
            Response = reader.GetString(reader.GetOrdinal("response")),
            Metadata = metadata,
            Error = reader.GetString(reader.GetOrdinal("error")),
            Attempts = reader.GetInt32(reader.GetOrdinal("attempts")),
            FailedAtUtc = DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("failed_at_utc")))
        };
    }
}
