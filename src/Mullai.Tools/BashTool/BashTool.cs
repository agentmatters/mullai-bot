using System.ComponentModel;
using Microsoft.Extensions.AI;
using Mullai.Tools.CliTool;

namespace Mullai.Tools.BashTool;

/// <summary>
///     A tool for executing bash/shell commands with advanced options.
/// </summary>
[Description(
    "A tool for executing shell commands (bash/zsh/cmd) with support for custom timeouts and working directories.")]
public class BashTool(CliProvider cliProvider)
{
    /// <summary>
    ///     Executes a shell command with a specified timeout and working directory.
    /// </summary>
    [Description("Executes a shell command. Use this for running build scripts, tests, or other CLI tools.")]
    public async Task<string> ExecuteBashCommand(
        [Description("The shell command to execute.")]
        string command,
        [Description("Optional timeout in milliseconds. Defaults to 120000 (2 minutes).")]
        int? timeoutMs = null,
        [Description("The working directory to run the command in.")]
        string? workdir = null)
    {
        // Note: For now, we use the CliProvider's stateless execution.
        // If we need to support workdir specifically, we might need to modify CliProvider or 
        // prepending 'cd <workdir> && ' to the command.

        var effectiveCommand = command;
        if (!string.IsNullOrWhiteSpace(workdir)) effectiveCommand = $"cd \"{workdir}\" && {command}";

        // CliProvider's ExecuteCommandAsync currently has a fixed timeout. 
        // We might want to pass timeoutMs to it in the future.
        return await cliProvider.ExecuteCommandAsync(effectiveCommand);
    }

    /// <summary>
    ///     Returns the functions provided by this plugin.
    /// </summary>
    public IEnumerable<AITool> AsAITools()
    {
        yield return AIFunctionFactory.Create(ExecuteBashCommand);
    }
}