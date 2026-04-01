using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Moq;
using Moq.Protected;
using Mullai.Providers.LLMProviders.Mistral;

namespace Mullai.Providers.Tests.LLMProviders.Mistral;

public class MistralChatClientTests
{
    private readonly Uri _endpoint = new("https://api.mistral.ai/v1/chat/completions");
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly HttpClient _httpClient;

    public MistralChatClientTests()
    {
        _handlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_handlerMock.Object);
    }

    [Fact]
    public async Task GetResponseAsync_ReturnsMappedResponse()
    {
        // Arrange
        var mistralResponse = new MistralChatResponse
        {
            Id = "msg-123",
            Choices = new List<MistralChoice>
            {
                new() { Message = new MistralChatMessage("assistant", "Hello from mocked Mistral") }
            }
        };

        var json = JsonSerializer.Serialize(mistralResponse);

        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        var client = new MistralChatClient(_httpClient, _endpoint)
        {
            OnBeforeRequest = req => req.Headers.Add("Authorization", "Bearer test-key")
        };

        // Act
        var response = await client.GetResponseAsync(new[] { new ChatMessage(ChatRole.User, "hi") });

        // Assert
        Assert.Equal("Hello from mocked Mistral", response.Messages[0].Text);
        Assert.Equal("msg-123", response.ResponseId);

        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Headers.Authorization != null &&
                req.Headers.Authorization.Scheme == "Bearer" &&
                req.Headers.Authorization.Parameter == "test-key"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetStreamingResponseAsync_ParsesSSESuccessfully()
    {
        // Arrange
        var sseData = new StringBuilder();
        sseData.AppendLine("data: {\"id\":\"1\",\"choices\":[{\"delta\":{\"content\":\"H\",\"role\":\"assistant\"}}]}");
        sseData.AppendLine("");
        sseData.AppendLine("data: {\"id\":\"1\",\"choices\":[{\"delta\":{\"content\":\"ello\"}}]}");
        sseData.AppendLine("");
        sseData.AppendLine("data: [DONE]");
        sseData.AppendLine("");

        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(sseData.ToString(), Encoding.UTF8, "text/event-stream")
            });

        var client = new MistralChatClient(_httpClient, _endpoint);

        // Act
        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in client.GetStreamingResponseAsync(new[] { new ChatMessage(ChatRole.User, "hi") }))
            updates.Add(update);

        // Assert
        Assert.Equal(2, updates.Count);
        Assert.Equal("H", updates[0].Text);
        Assert.Equal("ello", updates[1].Text);
        Assert.Equal(ChatRole.Assistant, updates[0].Role);
    }
}