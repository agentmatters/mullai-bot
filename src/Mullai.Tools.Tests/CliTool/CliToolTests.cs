using Mullai.Tools.CliTool;
using Xunit;
using FluentAssertions;
using System.Threading.Tasks;
using System.Linq;

namespace Mullai.Tools.Tests.CliTool;

public class CliToolTests
{
    private readonly Mullai.Tools.CliTool.CliTool _tool;

    public CliToolTests()
    {
        var provider = new CliProvider();
        _tool = new Mullai.Tools.CliTool.CliTool(provider);
    }

    [Fact]
    public async Task ExecuteCommandAsync_DelegatesToProvider()
    {
        // Arrange
        var command = "echo \"test\"";

        // Act
        var result = await _tool.ExecuteCommandAsync(command);

        // Assert
        result.Should().Contain("test");
    }

    [Fact]
    public void AsAITools_ReturnsExpectedFunctions()
    {
        // Act
        var tools = _tool.AsAITools().ToList();

        // Assert
        tools.Should().NotBeNull();
        tools.Should().HaveCount(4);
        
        var toolNames = tools.Select(t => t.Name).ToList();
        toolNames.Should().Contain("ExecuteCommand");
        toolNames.Should().Contain("CreateSession");
        toolNames.Should().Contain("ExecuteSessionCommand");
        toolNames.Should().Contain("CloseSession");
    }
}
