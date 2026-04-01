using Mullai.Tools.CodeSearchTool;
using Mullai.Tools.WebTool;

namespace Mullai.Tools.Tests.WebTool;

public class WebToolTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly Tools.WebTool.WebTool _tool;

    public WebToolTests()
    {
        _httpClient = new HttpClient();
        var provider = new WebProvider(_httpClient);
        _tool = new Tools.WebTool.WebTool(provider);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    [Fact]
    public async Task SearchWeb_WithRealQuery_ReturnsResults()
    {
        // Act
        var result = await _tool.SearchWeb("Mullai AI Agent");

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result));
        Assert.DoesNotContain("Error:", result);
    }

    [Fact]
    public async Task FetchUrl_WithRealUrl_ReturnsContent()
    {
        // Act
        var result = await _tool.FetchUrl("https://www.google.com");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("<html", result.ToLower());
        Assert.DoesNotContain("Error:", result);
    }
}

public class CodeSearchToolTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly CodeSearchTool.CodeSearchTool _tool;

    public CodeSearchToolTests()
    {
        _httpClient = new HttpClient();
        var provider = new CodeSearchProvider(_httpClient);
        _tool = new CodeSearchTool.CodeSearchTool(provider);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    [Fact]
    public async Task SearchCode_WithRealQuery_ReturnsResults()
    {
        // Act
        var result = await _tool.SearchCode("React useState hook examples");

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result));
        Assert.DoesNotContain("Error:", result);
    }
}