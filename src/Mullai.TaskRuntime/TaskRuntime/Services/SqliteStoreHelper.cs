using Microsoft.Data.Sqlite;

namespace Mullai.TaskRuntime.Services;

public static class SqliteStoreHelper
{
    public static string ResolveDatabasePath()
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var configDir = Path.Combine(homeDir, ".mullai");
        Directory.CreateDirectory(configDir);
        return Path.Combine(configDir, "mullai.db");
    }

    public static SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection($"Data Source={ResolveDatabasePath()};Cache=Shared");
        connection.Open();
        return connection;
    }
}
