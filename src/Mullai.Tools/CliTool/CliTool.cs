using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace Mullai.Tools.CliTool;

/// <summary>
///     The agent plugin that provides command-line execution capabilities.
/// </summary>
/// <param name="cliProvider">The CLI provider to execute commands.</param>
public class CliTool(CliProvider cliProvider)
{
    /// <summary>
    ///     Creates a persistent CLI shell session and returns its unique ID.
    /// </summary>
    /// <returns>The unique CLI session ID string.</returns>
    [Description(
        "Creates a persistent CLI shell session (e.g., bash or zsh) and returns its unique ID. Use this ID for subsequent commands to maintain state, environment variables, and working directory.")]
    public string CreateCliSession()
    {
        return cliProvider.CreateSession();
    }

    /// <summary>
    ///     Executes a command in a specific CLI shell session.
    /// </summary>
    /// <param name="sessionId">The CLI session ID.</param>
    /// <param name="command">The command string to execute.</param>
    /// <returns>The combined standard output and error of the command execution.</returns>
    [Description(
        "Executes a command in a specific persistent CLI shell session. State is maintained across calls with the same session ID. Direct output and error messages are returned.")]
    public async Task<string> ExecuteCliSessionCommand(
        [Description("The unique ID of the CLI shell session to run the command in.")]
        string sessionId,
        [Description("The shell command string to execute.")]
        string command)
    {
        return await cliProvider.ExecuteSessionCommandAsync(sessionId, command);
    }

    /// <summary>
    ///     Closes a given CLI shell session.
    /// </summary>
    /// <param name="sessionId">The CLI session ID to close.</param>
    /// <returns>Confirmation message.</returns>
    [Description("Closes a persistent CLI shell session when it is no longer needed to free up system resources.")]
    public string CloseCliSession([Description("The unique ID of the CLI shell session to close.")] string sessionId)
    {
        return cliProvider.CloseSession(sessionId);
    }

    /// <summary>
    ///     Executes a command in a new, isolated CLI shell session and returns its output.
    ///     State is NOT saved after this command completes.
    /// </summary>
    /// <param name="command">The command string to execute.</param>
    /// <returns>The combined standard output and error of the command execution.</returns>
    [Description(
        "Executes a command in a new, isolated, and temporary CLI shell session. Note: This does not maintain state for future calls.")]
    public async Task<string> ExecuteCliCommand([Description("The shell command string to execute.")] string command)
    {
        return await cliProvider.ExecuteCommandAsync(command);
    }

    /// <summary>
    ///     Returns the functions provided by this plugin.
    /// </summary>
    /// <returns>The functions provided by this plugin.</returns>
    public IEnumerable<AITool> AsAITools()
    {
        yield return AIFunctionFactory.Create(CreateCliSession);
        yield return AIFunctionFactory.Create(ExecuteCliSessionCommand);
        yield return AIFunctionFactory.Create(CloseCliSession);
        yield return AIFunctionFactory.Create(ExecuteCliCommand);
    }
}