using Microsoft.Data.Sqlite;
using Mullai.Abstractions.WorkflowState;

namespace Mullai.TaskRuntime.Services;

public sealed class SqliteWorkflowStateStore : IWorkflowStateStore
{
    private const string InitSql = """
        CREATE TABLE IF NOT EXISTS workflow_state (
            workflow_id TEXT NOT NULL,
            state_key TEXT NOT NULL,
            json_value TEXT NOT NULL,
            updated_at_utc TEXT NOT NULL,
            PRIMARY KEY (workflow_id, state_key)
        );
        CREATE INDEX IF NOT EXISTS idx_workflow_state_workflow ON workflow_state(workflow_id);
        """;

    public SqliteWorkflowStateStore()
    {
        using var connection = SqliteStoreHelper.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = InitSql;
        command.ExecuteNonQuery();
    }

    public Task<WorkflowStateRecord?> GetAsync(string workflowId, string key, CancellationToken cancellationToken = default)
    {
        using var connection = SqliteStoreHelper.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM workflow_state WHERE workflow_id = $workflowId AND state_key = $key";
        command.Parameters.AddWithValue("$workflowId", workflowId);
        command.Parameters.AddWithValue("$key", key);
        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return Task.FromResult<WorkflowStateRecord?>(null);
        }

        return Task.FromResult<WorkflowStateRecord?>(ReadRecord(reader));
    }

    public Task<IReadOnlyCollection<WorkflowStateRecord>> GetAllAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        using var connection = SqliteStoreHelper.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM workflow_state WHERE workflow_id = $workflowId ORDER BY state_key";
        command.Parameters.AddWithValue("$workflowId", workflowId);
        using var reader = command.ExecuteReader();
        var results = new List<WorkflowStateRecord>();
        while (reader.Read())
        {
            results.Add(ReadRecord(reader));
        }

        return Task.FromResult<IReadOnlyCollection<WorkflowStateRecord>>(results);
    }

    public Task UpsertAsync(string workflowId, string key, string jsonValue, CancellationToken cancellationToken = default)
    {
        using var connection = SqliteStoreHelper.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO workflow_state (workflow_id, state_key, json_value, updated_at_utc)
            VALUES ($workflowId, $key, $jsonValue, $updatedAtUtc)
            ON CONFLICT(workflow_id, state_key) DO UPDATE SET
                json_value = excluded.json_value,
                updated_at_utc = excluded.updated_at_utc;
            """;
        command.Parameters.AddWithValue("$workflowId", workflowId);
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$jsonValue", jsonValue);
        command.Parameters.AddWithValue("$updatedAtUtc", DateTimeOffset.UtcNow.ToString("O"));
        command.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string workflowId, string key, CancellationToken cancellationToken = default)
    {
        using var connection = SqliteStoreHelper.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM workflow_state WHERE workflow_id = $workflowId AND state_key = $key";
        command.Parameters.AddWithValue("$workflowId", workflowId);
        command.Parameters.AddWithValue("$key", key);
        command.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    private static WorkflowStateRecord ReadRecord(SqliteDataReader reader)
    {
        return new WorkflowStateRecord
        {
            WorkflowId = reader.GetString(reader.GetOrdinal("workflow_id")),
            Key = reader.GetString(reader.GetOrdinal("state_key")),
            JsonValue = reader.GetString(reader.GetOrdinal("json_value")),
            UpdatedAtUtc = DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("updated_at_utc")))
        };
    }
}
