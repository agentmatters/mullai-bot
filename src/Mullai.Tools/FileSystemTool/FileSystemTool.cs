using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace Mullai.Tools.FileSystemTool;

/// <summary>
///     The agent plugin that provides file system capabilities.
/// </summary>
[Description("A tool for performing file system operations like reading from and writing to files.")]
public class FileSystemTool(FileSystemProvider fileSystemProvider)
{
    /// <summary>
    ///     Reads the content of a file.
    /// </summary>
    /// <param name="filePath">The absolute or relative path to the file.</param>
    /// <returns>The content of the file or an error message.</returns>
    [Description("Reads the entire content of a specified file and returns it as a string.")]
    public async Task<string> ReadFileSystemFile(
        [Description("The absolute or relative path to the file to be read.")]
        string filePath)
    {
        return await fileSystemProvider.ReadFileAsync(filePath);
    }

    /// <summary>
    ///     Writes content to a file, overwriting the existing file or creating a new one.
    /// </summary>
    /// <param name="filePath">The absolute or relative path to the file.</param>
    /// <param name="content">The content to write to the file.</param>
    /// <returns>A success or error message.</returns>
    [Description(
        "Writes the provided content to a file. Warning: This will overwrite the existing file or create a new one if it does not exist.")]
    public async Task<string> WriteFileSystemFile(
        [Description("The absolute or relative path to the file where content will be written.")]
        string filePath,
        [Description("The text content to be written to the file.")]
        string content)
    {
        return await fileSystemProvider.WriteFileAsync(filePath, content);
    }

    /// <summary>
    ///     Searches for files matching a glob pattern.
    /// </summary>
    [Description(
        "Searches for files matching a glob pattern (e.g., '*.cs', '**/*.txt') starting from a root directory.")]
    public string GlobSearch(
        [Description("The root directory to start the search.")]
        string rootPath,
        [Description("The glob pattern to match.")]
        string pattern)
    {
        return fileSystemProvider.GlobSearch(rootPath, pattern);
    }

    /// <summary>
    ///     Searches for text within files in a directory.
    /// </summary>
    [Description(
        "Searches for a specific text or regex pattern within all files in a directory and its subdirectories.")]
    public string GrepSearch(
        [Description("The directory to search in.")]
        string directoryPath,
        [Description("The text or regex pattern to search for.")]
        string query,
        [Description("Whether the query is a regular expression.")]
        bool isRegex = false,
        [Description("Whether the search should be case-insensitive.")]
        bool caseInsensitive = true)
    {
        return fileSystemProvider.GrepSearch(directoryPath, query, isRegex, caseInsensitive);
    }

    /// <summary>
    ///     Edits a file by replacing a specific string.
    /// </summary>
    [Description(
        "Modifies a file by replacing a target string with new content. This is useful for precise edits without overwriting the whole file.")]
    public async Task<string> EditFile(
        [Description("The path to the file to edit.")]
        string filePath,
        [Description("The exact text segment to be replaced.")]
        string targetContent,
        [Description("The new text to replace the target segment with.")]
        string replacementContent)
    {
        return await fileSystemProvider.EditFileAsync(filePath, targetContent, replacementContent);
    }

    /// <summary>
    ///     Truncates a file to a specific number of lines.
    /// </summary>
    [Description("Reduces a file's size by keeping only the first N lines.")]
    public async Task<string> TruncateFile(
        [Description("The path to the file to truncate.")]
        string filePath,
        [Description("The maximum number of lines to keep.")]
        int maxLines)
    {
        return await fileSystemProvider.TruncateFileAsync(filePath, maxLines);
    }

    /// <summary>
    ///     Applies a patch to a file.
    /// </summary>
    [Description("Applies a patch or set of changes to a file. Experimental.")]
    public async Task<string> ApplyPatch(
        [Description("The path to the file to patch.")]
        string filePath,
        [Description("The patch content to apply.")]
        string patchContent)
    {
        return await fileSystemProvider.ApplyPatchAsync(filePath, patchContent);
    }

    /// <summary>
    ///     Returns the functions provided by this plugin.
    /// </summary>
    /// <returns>The functions provided by this plugin.</returns>
    public IEnumerable<AITool> AsAITools()
    {
        yield return AIFunctionFactory.Create(ReadFileSystemFile);
        yield return AIFunctionFactory.Create(WriteFileSystemFile);
        yield return AIFunctionFactory.Create(GlobSearch);
        yield return AIFunctionFactory.Create(GrepSearch);
        yield return AIFunctionFactory.Create(EditFile);
        yield return AIFunctionFactory.Create(TruncateFile);
        yield return AIFunctionFactory.Create(ApplyPatch);
    }
}