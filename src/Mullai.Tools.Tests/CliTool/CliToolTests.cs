using Mullai.Tools.CliTool;

namespace Mullai.Tools.Tests.CliTool;

public class CliToolTests
{
    private readonly Tools.CliTool.CliTool _tool;

    public CliToolTests()
    {
        var provider = new CliProvider();
        _tool = new Tools.CliTool.CliTool(provider);
    }

    [Fact]
    public async Task ExecuteCliCommand_DelegatesToProvider()
    {
        // Arrange
        var command = "echo \"test\"";

        // Act
        var result = await _tool.ExecuteCliCommand(command);

        // Assert
        Assert.Contains("test", result);
    }

    [Fact]
    public void AsAITools_ReturnsExpectedFunctions()
    {
        // Act
        var tools = _tool.AsAITools().ToList();

        // Assert
        Assert.NotNull(tools);
        Assert.Equal(4, tools.Count);

        var toolNames = tools.Select(t => t.Name).ToList();
        Assert.Contains("ExecuteCliCommand", toolNames);
        Assert.Contains("CreateCliSession", toolNames);
        Assert.Contains("ExecuteCliSessionCommand", toolNames);
        Assert.Contains("CloseCliSession", toolNames);
    }
}