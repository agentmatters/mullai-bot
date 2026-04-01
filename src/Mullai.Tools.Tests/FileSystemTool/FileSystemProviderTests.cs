using Mullai.Tools.FileSystemTool;

namespace Mullai.Tools.Tests.FileSystemTool;

public class FileSystemProviderTests : IDisposable
{
    private readonly FileSystemProvider _provider;
    private readonly string _testDirectory;

    public FileSystemProviderTests()
    {
        _provider = new FileSystemProvider();
        _testDirectory = Path.Combine(Path.GetTempPath(), "MullaiFileSystemProviderTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory)) Directory.Delete(_testDirectory, true);
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
        Assert.Contains("Successfully", result);
        Assert.True(File.Exists(filePath));
        var writtenContent = await File.ReadAllTextAsync(filePath);
        Assert.Equal(content, writtenContent);
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
        Assert.Contains("Successfully", result);
        var writtenContent = await File.ReadAllTextAsync(filePath);
        Assert.Equal(content, writtenContent);
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
        Assert.Equal(expectedContent, result);
    }

    [Fact]
    public async Task ReadFileAsync_WithNonExistentFile_ReturnsErrorMessage()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "doesnotexist.txt");

        // Act
        var result = await _provider.ReadFileAsync(filePath);

        // Assert
        Assert.Contains("Error: File not found", result);
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
        Assert.Contains("[DIR]  subdir", result);
        Assert.Contains("[FILE] a.txt", result);
        Assert.Contains("[FILE] b.txt", result);
    }

    [Fact]
    public void ListDirectory_WithNonExistentDirectory_ReturnsErrorMessage()
    {
        // Arrange
        var badDirectory = Path.Combine(_testDirectory, "bad_dir");

        // Act
        var result = _provider.ListDirectory(badDirectory);

        // Assert
        Assert.Contains("Error: Directory not found", result);
    }

    [Fact]
    public void GlobSearch_WithMatchingPattern_ReturnsMatches()
    {
        // Arrange
        var file1 = Path.Combine(_testDirectory, "test1.txt");
        var file2 = Path.Combine(_testDirectory, "test2.log");
        File.WriteAllText(file1, "1");
        File.WriteAllText(file2, "2");

        // Act
        var result = _provider.GlobSearch(_testDirectory, "*.txt");

        // Assert
        Assert.Contains("test1.txt", result);
        Assert.DoesNotContain("test2.log", result);
    }

    [Fact]
    public void GrepSearch_WithMatchingContent_ReturnsLines()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "grep.txt");
        File.WriteAllText(filePath, "line1\nmatch this\nline3");

        // Act
        var result = _provider.GrepSearch(_testDirectory, "match this");

        // Assert
        Assert.Contains("grep.txt:2: match this", result);
    }

    [Fact]
    public async Task EditFileAsync_WithValidTarget_ReplacesContent()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "edit.txt");
        await File.WriteAllTextAsync(filePath, "Hello World");

        // Act
        var result = await _provider.EditFileAsync(filePath, "World", "Mullai");

        // Assert
        Assert.Contains("Successfully", result);
        var content = await File.ReadAllTextAsync(filePath);
        Assert.Equal("Hello Mullai", content);
    }

    [Fact]
    public async Task TruncateFileAsync_WithMoreLines_Truncates()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "truncate.txt");
        await File.WriteAllLinesAsync(filePath, new[] { "1", "2", "3", "4", "5" });

        // Act
        var result = await _provider.TruncateFileAsync(filePath, 2);

        // Assert
        Assert.Contains("Successfully", result);
        var lines = await File.ReadAllLinesAsync(filePath);
        Assert.Equal(2, lines.Length);
        Assert.Equal("1", lines[0]);
        Assert.Equal("2", lines[1]);
    }
}