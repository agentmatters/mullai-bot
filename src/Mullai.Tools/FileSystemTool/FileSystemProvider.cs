using System.Text.RegularExpressions;

namespace Mullai.Tools.FileSystemTool;

/// <summary>
///     A provider for executing file system operations.
/// </summary>
public class FileSystemProvider
{
    /// <summary>
    ///     Reads the content of a file.
    /// </summary>
    /// <param name="filePath">The absolute or relative path to the file.</param>
    /// <returns>The content of the file.</returns>
    public async Task<string> ReadFileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return $"Error: File not found at '{filePath}'.";

            var content = await File.ReadAllTextAsync(filePath);
            return content;
        }
        catch (Exception ex)
        {
            return $"Failed to read file. Error: {ex.Message}";
        }
    }

    /// <summary>
    ///     Writes content to a file. If the file already exists, it is overwritten.
    /// </summary>
    /// <param name="filePath">The absolute or relative path to the file.</param>
    /// <param name="content">The content to write.</param>
    /// <returns>A message indicating success or failure.</returns>
    public async Task<string> WriteFileAsync(string filePath, string content)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            await File.WriteAllTextAsync(filePath, content);
            return $"Successfully wrote to file '{filePath}'.";
        }
        catch (Exception ex)
        {
            return $"Failed to write to file. Error: {ex.Message}";
        }
    }

    /// <summary>
    ///     Lists the contents (files and directories) of a specified directory.
    /// </summary>
    /// <param name="directoryPath">The path to the directory.</param>
    /// <returns>A formatted string of the directory contents.</returns>
    public string ListDirectory(string directoryPath)
    {
        try
        {
            if (!Directory.Exists(directoryPath)) return $"Error: Directory not found at '{directoryPath}'.";

            var directories = Directory.GetDirectories(directoryPath);
            var files = Directory.GetFiles(directoryPath);

            var result = $"Directory Listing for '{directoryPath}':\n\n";
            result += "Directories:\n";
            if (directories.Length == 0) result += "  (none)\n";
            foreach (var dir in directories.OrderBy(d => d)) result += $"  [DIR]  {Path.GetFileName(dir)}\n";

            result += "\nFiles:\n";
            if (files.Length == 0) result += "  (none)\n";
            foreach (var file in files.OrderBy(f => f)) result += $"  [FILE] {Path.GetFileName(file)}\n";

            return result;
        }
        catch (Exception ex)
        {
            return $"Failed to list directory. Error: {ex.Message}";
        }
    }

    /// <summary>
    ///     Searches for files matching a glob pattern.
    /// </summary>
    /// <param name="rootPath">The root directory to start the search.</param>
    /// <param name="pattern">The glob pattern (e.g., "*.cs", "**/*.txt").</param>
    /// <returns>A list of matching file paths.</returns>
    public string GlobSearch(string rootPath, string pattern)
    {
        try
        {
            if (!Directory.Exists(rootPath)) return $"Error: Directory not found at '{rootPath}'.";

            // Note: Standard .NET Directory.GetFiles supports simple patterns. 
            // For complex globbing like **/ we might need a library, but for now we'll use SearchOption.AllDirectories.
            var searchOption = pattern.Contains("**/") ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var cleanPattern = pattern.Replace("**/", "");

            var files = Directory.GetFiles(rootPath, cleanPattern, searchOption);
            var result = $"Glob Search results for '{pattern}' in '{rootPath}':\n";
            if (files.Length == 0) result += "  (no matches found)\n";
            foreach (var file in files.OrderBy(f => f)) result += $"  {Path.GetRelativePath(rootPath, file)}\n";

            return result;
        }
        catch (Exception ex)
        {
            return $"Failed to perform glob search. Error: {ex.Message}";
        }
    }

    /// <summary>
    ///     Searches for a specific pattern within files in a directory.
    /// </summary>
    public string GrepSearch(string directoryPath, string query, bool isRegex = false, bool caseInsensitive = true)
    {
        try
        {
            if (!Directory.Exists(directoryPath)) return $"Error: Directory not found at '{directoryPath}'.";

            var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);
            var results = new List<string>();
            var options = caseInsensitive ? RegexOptions.IgnoreCase : RegexOptions.None;

            foreach (var file in files)
                try
                {
                    var lines = File.ReadAllLines(file);
                    for (var i = 0; i < lines.Length; i++)
                    {
                        var match = false;
                        if (isRegex)
                            match = Regex.IsMatch(lines[i], query, options);
                        else
                            match = lines[i].Contains(query,
                                caseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

                        if (match)
                            results.Add($"{Path.GetRelativePath(directoryPath, file)}:{i + 1}: {lines[i].Trim()}");
                    }
                }
                catch
                {
                    /* skip files that can't be read */
                }

            if (results.Count == 0) return "No matches found.";
            return string.Join("\n", results.Take(100)) + (results.Count > 100 ? "\n... (truncated)" : "");
        }
        catch (Exception ex)
        {
            return $"Failed to perform grep search. Error: {ex.Message}";
        }
    }

    /// <summary>
    ///     Edits a file by replacing a target string with a replacement string.
    /// </summary>
    public async Task<string> EditFileAsync(string filePath, string targetContent, string replacementContent)
    {
        try
        {
            if (!File.Exists(filePath)) return $"Error: File not found at '{filePath}'.";

            var content = await File.ReadAllTextAsync(filePath);
            if (!content.Contains(targetContent))
                return
                    $"Error: Target content not found in file '{filePath}'. Make sure you provide the exact text to replace.";

            var newContent = content.Replace(targetContent, replacementContent);
            await File.WriteAllTextAsync(filePath, newContent);
            return $"Successfully updated file '{filePath}'.";
        }
        catch (Exception ex)
        {
            return $"Failed to edit file. Error: {ex.Message}";
        }
    }

    /// <summary>
    ///     Truncates a file to a maximum number of lines.
    /// </summary>
    public async Task<string> TruncateFileAsync(string filePath, int maxLines)
    {
        try
        {
            if (!File.Exists(filePath)) return $"Error: File not found at '{filePath}'.";

            var lines = await File.ReadAllLinesAsync(filePath);
            if (lines.Length <= maxLines)
                return $"File '{filePath}' is already within the line limit ({lines.Length} lines).";

            var truncatedLines = lines.Take(maxLines);
            await File.WriteAllLinesAsync(filePath, truncatedLines);
            return $"Successfully truncated file '{filePath}' to {maxLines} lines.";
        }
        catch (Exception ex)
        {
            return $"Failed to truncate file. Error: {ex.Message}";
        }
    }

    /// <summary>
    ///     Applies a unified diff patch to a file.
    /// </summary>
    public async Task<string> ApplyPatchAsync(string filePath, string patchContent)
    {
        // Simple implementation: for now we assume the patch is a full replacement 
        // OR we try to use the 'patch' command if available via CliProvider.
        // But to keep it simple and provider-focused, we'll try a very basic line-based patch if it looks like one.

        try
        {
            if (!File.Exists(filePath)) return $"Error: File not found at '{filePath}'.";

            // For now, let's just use EditFileAsync logic if it's not a real diff, 
            // or provide a placeholder for real patching.
            return "Patching functionality is currently experimental. Please use EditFile for precise changes.";
        }
        catch (Exception ex)
        {
            return $"Failed to apply patch. Error: {ex.Message}";
        }
    }
}