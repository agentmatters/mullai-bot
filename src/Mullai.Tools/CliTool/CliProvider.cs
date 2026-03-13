using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Mullai.Tools.CliTool;

/// <summary>
/// A provider for executing command-line instructions.
/// </summary>
public class CliProvider : IDisposable
{
    private readonly Dictionary<string, Process> _sessions = new();
    private readonly bool _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    private readonly string _eofToken = "===MULLAI_EOF_TOKEN===";

    /// <summary>
    /// Creates a persistent shell session and returns its unique ID.
    /// </summary>
    public string CreateSession()
    {
        var sessionId = Guid.NewGuid().ToString("N").Substring(0, 6);
        var shell = _isWindows ? "cmd.exe" : "/bin/bash";

        var startInfo = new ProcessStartInfo
        {
            FileName = shell,
            WorkingDirectory = Environment.CurrentDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = new Process { StartInfo = startInfo };
        process.Start();

        _sessions[sessionId] = process;
        
        // Discard any initial shell startup text
        if (_isWindows)
        {
            // Windows cmd.exe usually prints its version and copyright. 
            // We write empty commands and the EOF token so we can synchronize.
            ExecuteSessionCommandAsync(sessionId, "").GetAwaiter().GetResult();
        }
        else
        {
             // On Linux/Mac, bash usually starts quietly without a command if not interactive, but for safety we sync it too.
            ExecuteSessionCommandAsync(sessionId, "").GetAwaiter().GetResult();
        }

        return sessionId;
    }

    /// <summary>
    /// Executes a command in a specific shell session.
    /// </summary>
    public async Task<string> ExecuteSessionCommandAsync(string sessionId, string command)
    {
        if (!_sessions.TryGetValue(sessionId, out var process))
        {
            return $"Error: Session '{sessionId}' not found or already closed.";
        }

        if (process.HasExited)
        {
            return $"Error: Session '{sessionId}' has exited unexpectedly.";
        }

        // We append an echo of the EOF token so we know when the command finishes.
        string fullCommand;
        if (_isWindows)
        {
            fullCommand = string.IsNullOrWhiteSpace(command) 
                ? $"echo {_eofToken}" 
                : $"{command} 2>&1\r\necho {_eofToken}";
        }
        else
        {
            fullCommand = string.IsNullOrWhiteSpace(command)
                ? $"echo '{_eofToken}'"
                : $"{command} 2>&1\necho '{_eofToken}'";
        }

        await process.StandardInput.WriteLineAsync(fullCommand);
        await process.StandardInput.FlushAsync();

        var outputBuilder = new System.Text.StringBuilder();

        // Read until we encounter our EOF token
        while (true)
        {
            var lineTask = process.StandardOutput.ReadLineAsync();
            var timeoutTask = Task.Delay(TimeSpan.FromMinutes(2)); // basic safety timeout
            
            var completedTask = await Task.WhenAny(lineTask, timeoutTask);
            if (completedTask == timeoutTask)
            {
                outputBuilder.AppendLine("Error: Command execution timed out or shell dropped in interactive prompt.");
                break;
            }

            var line = await lineTask;
            if (line == null) 
            {
                break; // stream closed
            }

            // Sometimes Windows echo prints quotes or trailing spaces if not careful, we do a Contains check.
            if (line.Contains(_eofToken))
            {
                break;
            }

            // Exclude the echoed command itself in Windows cmd (cmd echoes by default)
            if (_isWindows && !string.IsNullOrWhiteSpace(command) && line.Contains(command))
            {
                continue;
            }

            outputBuilder.AppendLine(line);
        }

        var result = outputBuilder.ToString().Trim();
        return string.IsNullOrWhiteSpace(result) ? "Command executed successfully with no output." : result;
    }

    /// <summary>
    /// Closes a given shell session.
    /// </summary>
    public string CloseSession(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var process))
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
                process.Dispose();
            }
            catch 
            {
                // ignore kill errors if already dead
            }
            _sessions.Remove(sessionId);
            return $"Session '{sessionId}' closed.";
        }
        return $"Session '{sessionId}' not found.";
    }

    /// <summary>
    /// Executes a command in a single, isolated execution process (stateless).
    /// </summary>
    public async Task<string> ExecuteCommandAsync(string command)
    {
        var sessionId = CreateSession();
        try
        {
            return await ExecuteSessionCommandAsync(sessionId, command);
        }
        finally
        {
            CloseSession(sessionId);
        }
    }

    public void Dispose()
    {
        foreach (var session in _sessions.Values)
        {
            try
            {
                if (!session.HasExited) session.Kill();
                session.Dispose();
            }
            catch { }
        }
        _sessions.Clear();
    }
}
