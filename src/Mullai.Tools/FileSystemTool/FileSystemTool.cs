using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Mullai.Tools.FileSystemTool;

/// <summary>
/// The agent plugin that provides file system capabilities.
/// </summary>
/// <param name="fileSystemProvider">The file system provider to execute operations.</param>
public class FileSystemTool(FileSystemProvider fileSystemProvider)
{
    /// <summary>
    /// Reads the content of a file.
    /// </summary>
    /// <param name="filePath">The absolute or relative path to the file.</param>
    /// <returns>The content of the file or an error message.</returns>
    [Description("Reads the content of a file.")]
    public async Task<string> ReadFileAsync(
        [Description("The absolute or relative path to the file.")] string filePath)
    {
        return await fileSystemProvider.ReadFileAsync(filePath);
    }

    /// <summary>
    /// Writes content to a file, overwriting the existing file or creating a new one.
    /// </summary>
    /// <param name="filePath">The absolute or relative path to the file.</param>
    /// <param name="content">The content to write to the file.</param>
    /// <returns>A success or error message.</returns>
    [Description("Writes content to a file, overwriting the existing file or creating a new one if it does not exist.")]
    public async Task<string> WriteFileAsync(
        [Description("The absolute or relative path to the file.")] string filePath,
        [Description("The content to write to the file.")] string content)
    {
        return await fileSystemProvider.WriteFileAsync(filePath, content);
    }

    // /// <summary>
    // /// Lists the contents of a directory, including files and subdirectories.
    // /// </summary>
    // /// <param name="directoryPath">The absolute or relative path to the directory.</param>
    // /// <returns>A formatted string listing the directories and files.</returns>
    // [Description("Lists the contents of a directory, showing all files and subdirectories.")]
    // public Task<string> ListDirectoryAsync(
    //     [Description("The absolute or relative path to the directory.")] string directoryPath)
    // {
    //     return Task.FromResult(fileSystemProvider.ListDirectory(directoryPath));
    // }

    /// <summary>
    /// Returns the functions provided by this plugin.
    /// </summary>
    /// <returns>The functions provided by this plugin.</returns>
    public IEnumerable<AITool> AsAITools()
    {
        yield return AIFunctionFactory.Create(this.ReadFileAsync);
        yield return AIFunctionFactory.Create(this.WriteFileAsync);
        // yield return AIFunctionFactory.Create(this.ListDirectoryAsync);
    }
}
