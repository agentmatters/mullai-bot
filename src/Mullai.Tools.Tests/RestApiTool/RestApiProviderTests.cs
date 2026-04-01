using System.Net;
using Moq;
using Moq.Protected;
using Mullai.Tools.RestApiTool;
using Mullai.Tools.RestApiTool.Models;

namespace Mullai.Tools.Tests.RestApi;

public class RestApiProviderTests
{
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly HttpClient _httpClient;
    private readonly RestApiProvider _provider;

    public RestApiProviderTests()
    {
        _handlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_handlerMock.Object);
        _provider = new RestApiProvider(_httpClient);
    }

    [Fact]
    public async Task SendAsync_GetRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new RestApiRequest
        {
            Url = "https://api.example.com/test",
            Method = "GET"
        };

        var responseContent = "{\"message\": \"success\"}";
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent)
            });

        // Act
        var result = await _provider.SendAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(responseContent, result.Content);
    }

    [Fact]
    public async Task SendAsync_PostRequest_SendsBody()
    {
        // Arrange
        var request = new RestApiRequest
        {
            Url = "https://api.example.com/test",
            Method = "POST",
            Body = "{\"data\": \"test\"}",
            ContentType = "application/json"
        };

        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == HttpMethod.Post &&
                    r.Content != null),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Created,
                Content = new StringContent("{}")
            });

        // Act
        var result = await _provider.SendAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
    }

    [Fact]
    public async Task SendAsync_RequestFails_ReturnsError()
    {
        // Arrange
        var request = new RestApiRequest
        {
            Url = "https://invalid.url",
            Method = "GET"
        };

        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        // Act
        var result = await _provider.SendAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Connection failed", result.Error);
    }
}