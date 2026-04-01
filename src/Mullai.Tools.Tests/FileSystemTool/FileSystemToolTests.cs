using Mullai.Tools.FileSystemTool;

namespace Mullai.Tools.Tests.FileSystemTool;

public class FileSystemToolTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly Tools.FileSystemTool.FileSystemTool _tool;

    public FileSystemToolTests()
    {
        var provider = new FileSystemProvider();
        _tool = new Tools.FileSystemTool.FileSystemTool(provider);

        _testDirectory = Path.Combine(Path.GetTempPath(), "MullaiFileSystemToolTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory)) Directory.Delete(_testDirectory, true);
    }

    [Fact]
    public async Task WriteFileSystemFile_DelegatesToProvider()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "toolwrite.txt");
        var content = "Tool content";

        // Act
        var result = await _tool.WriteFileSystemFile(filePath, content);

        // Assert
        Assert.Contains("Successfully", result);
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public async Task ReadFileSystemFile_DelegatesToProvider()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "toolread.txt");
        await File.WriteAllTextAsync(filePath, "Read from tool");

        // Act
        var result = await _tool.ReadFileSystemFile(filePath);

        // Assert
        Assert.Equal("Read from tool", result);
    }

    [Fact]
    public void AsAITools_ReturnsExpectedFunctions()
    {
        // Act
        var tools = _tool.AsAITools().ToList();

        // Assert
        Assert.NotNull(tools);
        Assert.Equal(7, tools.Count);

        var toolNames = tools.Select(t => t.Name).ToList();
        Assert.Contains("ReadFileSystemFile", toolNames);
        Assert.Contains("WriteFileSystemFile", toolNames);
        Assert.Contains("GlobSearch", toolNames);
        Assert.Contains("GrepSearch", toolNames);
        Assert.Contains("EditFile", toolNames);
        Assert.Contains("TruncateFile", toolNames);
        Assert.Contains("ApplyPatch", toolNames);
    }
}