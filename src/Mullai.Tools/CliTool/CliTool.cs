using Microsoft.Extensions.AI;

namespace Mullai.Tools.CliTool;

/// <summary>
/// The agent plugin that provides command-line execution capabilities.
/// </summary>
/// <param name="cliProvider">The CLI provider to execute commands.</param>
public class CliTool(CliProvider cliProvider)
{
    /// <summary>
    /// Creates a persistent shell session and returns its unique ID.
    /// </summary>
    /// <returns>The unique session ID string.</returns>
    [System.ComponentModel.Description("Creates a persistent shell session and returns its unique ID. Use this ID for subsequent commands to maintain state.")]
    public string CreateSession()
    {
        return cliProvider.CreateSession();
    }

    /// <summary>
    /// Executes a command in a specific shell session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="command">The command string to execute.</param>
    /// <returns>The combined standard output and error of the command execution.</returns>
    [System.ComponentModel.Description("Executes a command in a specific persistent shell session. State is maintained across calls with the same session ID.")]
    public async Task<string> ExecuteSessionCommandAsync(
        [System.ComponentModel.Description("The ID of the session to run the command in.")] string sessionId, 
        [System.ComponentModel.Description("The command to run.")] string command)
    {
        return await cliProvider.ExecuteSessionCommandAsync(sessionId, command);
    }

    /// <summary>
    /// Closes a given shell session.
    /// </summary>
    /// <param name="sessionId">The session ID to close.</param>
    /// <returns>Confirmation message.</returns>
    [System.ComponentModel.Description("Closes a persistent shell session when it is no longer needed.")]
    public string CloseSession([System.ComponentModel.Description("The ID of the session to close.")] string sessionId)
    {
        return cliProvider.CloseSession(sessionId);
    }

    /// <summary>
    /// Executes a command in a new, isolated shell session and returns its output.
    /// State is NOT saved after this command completes.
    /// </summary>
    /// <param name="command">The command string to execute.</param>
    /// <returns>The combined standard output and error of the command execution.</returns>
    [System.ComponentModel.Description("Executes a command in a new, isolated shell session.")]
    public async Task<string> ExecuteCommandAsync([System.ComponentModel.Description("The command to run.")] string command)
    {
        return await cliProvider.ExecuteCommandAsync(command);
    }

    /// <summary>
    /// Returns the functions provided by this plugin.
    /// </summary>
    /// <returns>The functions provided by this plugin.</returns>
    public IEnumerable<AITool> AsAITools()
    {
        yield return AIFunctionFactory.Create(this.CreateSession);
        yield return AIFunctionFactory.Create(this.ExecuteSessionCommandAsync);
        yield return AIFunctionFactory.Create(this.CloseSession);
        yield return AIFunctionFactory.Create(this.ExecuteCommandAsync);
    }
}
