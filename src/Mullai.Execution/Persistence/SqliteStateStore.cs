using System.Text.Json;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.AI;
using Mullai.Abstractions.Orchestration;
using Mullai.Abstractions.Persistence;

namespace Mullai.Execution.Persistence;

public class SqliteStateStore : IStateStore
{
    private readonly string _connectionString;

    public SqliteStateStore(string? dbPath = null)
    {
        dbPath ??= GetDefaultDbPath();
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        EnsureDbDirectory();
        using var connection = new SqliteConnection(_connectionString);
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Sessions (
                SessionId TEXT PRIMARY KEY,
                History TEXT,
                Checkpoint TEXT,
                UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
            )");
    }

    private static string GetDefaultDbPath()
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var configDir = Path.Combine(homeDir, ".mullai");
        return Path.Combine(configDir, "mullai.db");
    }

    private void EnsureDbDirectory()
    {
        try
        {
            var builder = new SqliteConnectionStringBuilder(_connectionString);
            var dbPath = builder.DataSource;
            var dir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
        catch
        {
        }
    }

    public async Task SaveHistoryAsync(string sessionId, List<ChatMessage> history, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(history);
        using var connection = new SqliteConnection(_connectionString);
        await connection.ExecuteAsync(@"
            INSERT INTO Sessions (SessionId, History) VALUES (@sessionId, @json)
            ON CONFLICT(SessionId) DO UPDATE SET History = @json, UpdatedAt = CURRENT_TIMESTAMP", 
            new { sessionId, json });
    }

    public async Task<List<ChatMessage>> GetHistoryAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        var json = await connection.QueryFirstOrDefaultAsync<string>(
            "SELECT History FROM Sessions WHERE SessionId = @sessionId", new { sessionId });
        
        return json != null ? JsonSerializer.Deserialize<List<ChatMessage>>(json) ?? new() : new();
    }

    public async Task SaveCheckpointAsync(string sessionId, TaskGraph graph, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(graph);
        using var connection = new SqliteConnection(_connectionString);
        await connection.ExecuteAsync(@"
            INSERT INTO Sessions (SessionId, Checkpoint) VALUES (@sessionId, @json)
            ON CONFLICT(SessionId) DO UPDATE SET Checkpoint = @json, UpdatedAt = CURRENT_TIMESTAMP", 
            new { sessionId, json });
    }

    public async Task<TaskGraph?> GetCheckpointAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        var json = await connection.QueryFirstOrDefaultAsync<string>(
            "SELECT Checkpoint FROM Sessions WHERE SessionId = @sessionId", new { sessionId });
        
        return json != null ? JsonSerializer.Deserialize<TaskGraph>(json) : null;
    }

    public async Task ClearSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.ExecuteAsync("DELETE FROM Sessions WHERE SessionId = @sessionId", new { sessionId });
    }
}
