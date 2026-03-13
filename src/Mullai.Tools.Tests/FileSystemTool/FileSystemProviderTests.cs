using Mullai.Tools.FileSystemTool;
using Xunit;
using FluentAssertions;
using System.IO;
using System.Threading.Tasks;
using System;

namespace Mullai.Tools.Tests.FileSystemTool;

public class FileSystemProviderTests : IDisposable
{
    private readonly FileSystemProvider _provider;
    private readonly string _testDirectory;

    public FileSystemProviderTests()
    {
        _provider = new FileSystemProvider();
        _testDirectory = Path.Combine(Path.GetTempPath(), "MullaiFileSystemProviderTests_" + Guid.NewGuid().ToString());
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
    public async Task WriteFileAsync_WithNewFile_CreatesFileAndWritesContent()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        var content = "Hello FileSystemProvider!";

        // Act
        var result = await _provider.WriteFileAsync(filePath, content);

        // Assert
        result.Should().Contain("Successfully");
        File.Exists(filePath).Should().BeTrue();
        var writtenContent = await File.ReadAllTextAsync(filePath);
        writtenContent.Should().Be(content);
    }

    [Fact]
    public async Task WriteFileAsync_WithExistingFile_OverwritesContent()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "existing.txt");
        await File.WriteAllTextAsync(filePath, "Old Content");
        var content = "New Content!";

        // Act
        var result = await _provider.WriteFileAsync(filePath, content);

        // Assert
        result.Should().Contain("Successfully");
        var writtenContent = await File.ReadAllTextAsync(filePath);
        writtenContent.Should().Be(content);
    }

    [Fact]
    public async Task ReadFileAsync_WithExistingFile_ReturnsContent()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "readtest.txt");
        var expectedContent = "Content to read";
        await File.WriteAllTextAsync(filePath, expectedContent);

        // Act
        var result = await _provider.ReadFileAsync(filePath);

        // Assert
        result.Should().Be(expectedContent);
    }

    [Fact]
    public async Task ReadFileAsync_WithNonExistentFile_ReturnsErrorMessage()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "doesnotexist.txt");

        // Act
        var result = await _provider.ReadFileAsync(filePath);

        // Assert
        result.Should().Contain("Error: File not found");
    }

    [Fact]
    public void ListDirectory_WithExistingDirectory_ReturnsFormattedListing()
    {
        // Arrange
        var file1 = Path.Combine(_testDirectory, "a.txt");
        var file2 = Path.Combine(_testDirectory, "b.txt");
        var subDir = Path.Combine(_testDirectory, "subdir");
        File.WriteAllText(file1, "A");
        File.WriteAllText(file2, "B");
        Directory.CreateDirectory(subDir);

        // Act
        var result = _provider.ListDirectory(_testDirectory);

        // Assert
        result.Should().Contain("[DIR]  subdir");
        result.Should().Contain("[FILE] a.txt");
        result.Should().Contain("[FILE] b.txt");
    }

    [Fact]
    public void ListDirectory_WithNonExistentDirectory_ReturnsErrorMessage()
    {
        // Arrange
        var badDirectory = Path.Combine(_testDirectory, "bad_dir");

        // Act
        var result = _provider.ListDirectory(badDirectory);

        // Assert
        result.Should().Contain("Error: Directory not found");
    }
}
