using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Mullai.Abstractions;
using Mullai.Abstractions.Configuration;

namespace Mullai.Providers.Tests;

public class MullaiChatClientDynamicTests
{
    private readonly Mock<IMullaiConfigurationManager> _configManagerMock = new();
    private readonly Mock<IConfiguration> _configMock = new();
    private readonly HttpClient _httpClient = new();
    private readonly Mock<ILogger<MullaiChatClient>> _loggerMock = new();

    [Fact]
    public async Task GetResponseAsync_WithRequestContext_UsesOverride()
    {
        // Arrange
        var mockDefaultClient = new Mock<IChatClient>();
        var mockOverrideClient = new Mock<IChatClient>();

        var clients = new List<(string Label, IChatClient Client)>
        {
            ("Default/ModelA", mockDefaultClient.Object)
        };

        var mullaiClient = new MullaiChatClient(
            clients,
            _loggerMock.Object,
            _configManagerMock.Object,
            _configMock.Object,
            _httpClient);

        // Define the behavior for the override client (which is already in the list for this test)
        // Wait, if it's in the list, it should just pick it.
        mockDefaultClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Default Response")));

        mockOverrideClient
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Override Response")));

        // Add the override client to the list
        clients.Add(("Override/ModelB", mockOverrideClient.Object));
        mullaiClient.UpdateClients(clients);

        // Act & Assert - Without context
        var resp1 = await mullaiClient.GetResponseAsync(new[] { new ChatMessage(ChatRole.User, "hi") });
        Assert.Equal("Default Response", resp1.Messages[0].Text);

        // Act & Assert - With context
        MullaiRequestContext.Current = new MullaiRequestInfo { Provider = "Override", Model = "ModelB" };
        try
        {
            var resp2 = await mullaiClient.GetResponseAsync(new[] { new ChatMessage(ChatRole.User, "hi") });
            Assert.Equal("Override Response", resp2.Messages[0].Text);
        }
        finally
        {
            MullaiRequestContext.Current = null;
        }
    }

    [Fact]
    public async Task GetResponseAsync_WithModelOnlyOverride_FindsMatchingModel()
    {
        // Arrange
        var mockClientA = new Mock<IChatClient>();
        var mockClientB = new Mock<IChatClient>();

        var clients = new List<(string Label, IChatClient Client)>
        {
            ("Provider1/ModelA", mockClientA.Object),
            ("Provider2/ModelB", mockClientB.Object)
        };

        var mullaiClient = new MullaiChatClient(
            clients,
            _loggerMock.Object,
            _configManagerMock.Object,
            _configMock.Object,
            _httpClient);

        mockClientB
            .Setup(c => c.GetResponseAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "ModelB Response")));

        // Act
        MullaiRequestContext.Current = new MullaiRequestInfo { Model = "ModelB" };
        try
        {
            var response = await mullaiClient.GetResponseAsync(new[] { new ChatMessage(ChatRole.User, "hi") });
            Assert.Equal("ModelB Response", response.Messages[0].Text);
        }
        finally
        {
            MullaiRequestContext.Current = null;
        }
    }
}