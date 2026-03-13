using Mullai.Tools.CliTool;
using Xunit;
using FluentAssertions;
using System.Threading.Tasks;

namespace Mullai.Tools.Tests.CliTool;

public class CliProviderTests
{
    private readonly CliProvider _provider;

    public CliProviderTests()
    {
        _provider = new CliProvider();
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithValidCommand_ReturnsOutput()
    {
        // Arrange
        var command = "echo \"hello world\"";

        // Act
        var result = await _provider.ExecuteCommandAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("hello world");
    }

    [Fact]
    public async Task ExecuteCommandAsync_WithInvalidCommand_ReturnsError()
    {
        // Arrange
        var command = "this_command_does_not_exist_12345";

        // Act
        var result = await _provider.ExecuteCommandAsync(command);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("this_command_does_not_exist_12345");
    }

    [Fact]
    public async Task ExecuteSessionCommandAsync_MaintainsState()
    {
        // Arrange
        var sessionId = _provider.CreateSession();
        var testDirName = "mullai_test_session_dir_" + System.Guid.NewGuid().ToString("N");
        
        try
        {
            // Act
            await _provider.ExecuteSessionCommandAsync(sessionId, $"mkdir {testDirName}");
            await _provider.ExecuteSessionCommandAsync(sessionId, $"cd {testDirName}");
            
            // Depending on OS, 'pwd' or 'cd' without args prints current directory
            var pwdCmd = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows) ? "cd" : "pwd";
            var result = await _provider.ExecuteSessionCommandAsync(sessionId, pwdCmd);

            // Assert
            result.Should().Contain(testDirName);
        }
        finally
        {
             // Cleanup test directory
            await _provider.ExecuteSessionCommandAsync(sessionId, "cd ..");
            
            var rmCmd = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows) ? $"rmdir /s /q {testDirName}" : $"rm -rf {testDirName}";
            await _provider.ExecuteSessionCommandAsync(sessionId, rmCmd);
            
            _provider.CloseSession(sessionId);
        }
    }
}
