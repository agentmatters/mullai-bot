using Mullai.Tools.FileSystemTool;
using Xunit;
using FluentAssertions;
using System.Threading.Tasks;
using System.IO;
using System;
using System.Linq;

namespace Mullai.Tools.Tests.FileSystemTool;

public class FileSystemToolTests : IDisposable
{
    private readonly Mullai.Tools.FileSystemTool.FileSystemTool _tool;
    private readonly string _testDirectory;

    public FileSystemToolTests()
    {
        var provider = new FileSystemProvider();
        _tool = new Mullai.Tools.FileSystemTool.FileSystemTool(provider);
        
        _testDirectory = Path.Combine(Path.GetTempPath(), "MullaiFileSystemToolTests_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task WriteFileAsync_DelegatesToProvider()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "toolwrite.txt");
        var content = "Tool content";

        // Act
        var result = await _tool.WriteFileAsync(filePath, content);

        // Assert
        result.Should().Contain("Successfully");
        File.Exists(filePath).Should().BeTrue();
    }

    [Fact]
    public async Task ReadFileAsync_DelegatesToProvider()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "toolread.txt");
        await File.WriteAllTextAsync(filePath, "Read from tool");

        // Act
        var result = await _tool.ReadFileAsync(filePath);

        // Assert
        result.Should().Be("Read from tool");
    }

    [Fact]
    public void AsAITools_ReturnsExpectedFunctions()
    {
        // Act
        var tools = _tool.AsAITools().ToList();

        // Assert
        tools.Should().NotBeNull();
        tools.Should().HaveCount(2);
        
        var toolNames = tools.Select(t => t.Name).ToList();
        toolNames.Should().Contain("ReadFile");
        toolNames.Should().Contain("WriteFile");
    }
}
