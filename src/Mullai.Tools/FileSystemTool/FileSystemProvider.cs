using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Mullai.Tools.FileSystemTool;

/// <summary>
/// A provider for executing file system operations.
/// </summary>
public class FileSystemProvider
{
    /// <summary>
    /// Reads the content of a file.
    /// </summary>
    /// <param name="filePath">The absolute or relative path to the file.</param>
    /// <returns>The content of the file.</returns>
    public async Task<string> ReadFileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return $"Error: File not found at '{filePath}'.";
            }

            var content = await File.ReadAllTextAsync(filePath);
            return content;
        }
        catch (Exception ex)
        {
            return $"Failed to read file. Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Writes content to a file. If the file already exists, it is overwritten.
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
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(filePath, content);
            return $"Successfully wrote to file '{filePath}'.";
        }
        catch (Exception ex)
        {
            return $"Failed to write to file. Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Lists the contents (files and directories) of a specified directory.
    /// </summary>
    /// <param name="directoryPath">The path to the directory.</param>
    /// <returns>A formatted string of the directory contents.</returns>
    public string ListDirectory(string directoryPath)
    {
        try
        {
            if (!Directory.Exists(directoryPath))
            {
                return $"Error: Directory not found at '{directoryPath}'.";
            }

            var directories = Directory.GetDirectories(directoryPath);
            var files = Directory.GetFiles(directoryPath);

            var result = $"Directory Listing for '{directoryPath}':\n\n";
            result += "Directories:\n";
            if (directories.Length == 0) result += "  (none)\n";
            foreach (var dir in directories.OrderBy(d => d))
            {
                result += $"  [DIR]  {Path.GetFileName(dir)}\n";
            }

            result += "\nFiles:\n";
            if (files.Length == 0) result += "  (none)\n";
            foreach (var file in files.OrderBy(f => f))
            {
                result += $"  [FILE] {Path.GetFileName(file)}\n";
            }

            return result;
        }
        catch (Exception ex)
        {
            return $"Failed to list directory. Error: {ex.Message}";
        }
    }
}
