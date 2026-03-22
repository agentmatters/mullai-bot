using Microsoft.Data.Sqlite;
using Mullai.TaskRuntime.Abstractions;
using Mullai.TaskRuntime.Models;

namespace Mullai.TaskRuntime.Services;

public sealed class SqliteWorkflowRunEventStore : IWorkflowRunEventStore
{
    private const string InitSql = """
        CREATE TABLE IF NOT EXISTS workflow_run_events (
            id TEXT PRIMARY KEY,
            task_id TEXT NOT NULL,
            workflow_id TEXT,
            event_type TEXT NOT NULL,
            payload_json TEXT NOT NULL,
            created_at_utc TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS idx_workflow_run_events_task ON workflow_run_events(task_id, created_at_utc);
        CREATE INDEX IF NOT EXISTS idx_workflow_run_events_workflow ON workflow_run_events(workflow_id, created_at_utc);
        """;

    public SqliteWorkflowRunEventStore()
    {
        using var connection = SqliteStoreHelper.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = InitSql;
        command.ExecuteNonQuery();
    }

    public Task AppendAsync(WorkflowRunEvent runEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(runEvent);

        using var connection = SqliteStoreHelper.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO workflow_run_events (
                id,
                task_id,
                workflow_id,
                event_type,
                payload_json,
                created_at_utc
            ) VALUES (
                $id,
                $taskId,
                $workflowId,
                $eventType,
                $payloadJson,
                $createdAtUtc
            );
            """;
        command.Parameters.AddWithValue("$id", runEvent.Id);
        command.Parameters.AddWithValue("$taskId", runEvent.TaskId);
        command.Parameters.AddWithValue("$workflowId", (object?)runEvent.WorkflowId ?? DBNull.Value);
        command.Parameters.AddWithValue("$eventType", runEvent.EventType);
        command.Parameters.AddWithValue("$payloadJson", runEvent.PayloadJson);
        command.Parameters.AddWithValue("$createdAtUtc", runEvent.CreatedAtUtc.ToString("O"));
        command.ExecuteNonQuery();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<WorkflowRunEvent>> GetForTaskAsync(
        string taskId,
        int take = 200,
        CancellationToken cancellationToken = default)
    {
        using var connection = SqliteStoreHelper.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, task_id, workflow_id, event_type, payload_json, created_at_utc
            FROM workflow_run_events
            WHERE task_id = $taskId
            ORDER BY created_at_utc ASC
            LIMIT $take;
            """;
        command.Parameters.AddWithValue("$taskId", taskId);
        command.Parameters.AddWithValue("$take", Math.Max(1, take));

        using var reader = command.ExecuteReader();
        var results = new List<WorkflowRunEvent>();
        while (reader.Read())
        {
            results.Add(new WorkflowRunEvent
            {
                Id = reader.GetString(reader.GetOrdinal("id")),
                TaskId = reader.GetString(reader.GetOrdinal("task_id")),
                WorkflowId = reader.IsDBNull(reader.GetOrdinal("workflow_id"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("workflow_id")),
                EventType = reader.GetString(reader.GetOrdinal("event_type")),
                PayloadJson = reader.GetString(reader.GetOrdinal("payload_json")),
                CreatedAtUtc = DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("created_at_utc")))
            });
        }

        return Task.FromResult<IReadOnlyCollection<WorkflowRunEvent>>(results);
    }

    public Task RemoveByTaskIdsAsync(IEnumerable<string> taskIds, CancellationToken cancellationToken = default)
    {
        var ids = taskIds?.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList() ?? [];
        if (ids.Count == 0)
        {
            return Task.CompletedTask;
        }

        using var connection = SqliteStoreHelper.CreateConnection();
        using var command = connection.CreateCommand();
        var parameters = ids.Select((_, index) => $"$id{index}").ToArray();
        command.CommandText = $"DELETE FROM workflow_run_events WHERE task_id IN ({string.Join(",", parameters)});";
        for (var i = 0; i < ids.Count; i++)
        {
            command.Parameters.AddWithValue(parameters[i], ids[i]);
        }

        command.ExecuteNonQuery();
        return Task.CompletedTask;
    }
}
