using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Mullai.Providers.LLMProviders.Mistral;
using FluentAssertions;
using Xunit;

namespace Mullai.Providers.Tests.LLMProviders.Mistral;

public class MistralIntegrationTests
{
    private readonly IConfiguration _configuration;
    private readonly string? _apiKey;

    public MistralIntegrationTests()
    {
        _configuration = new ConfigurationBuilder()
            // .AddJsonFile("appsettings.json", optional: true)
            // .AddJsonFile("appsettings.Test.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        _apiKey = _configuration["Mistral:ApiKey"];
    }

    [SkippableFact]
    public async Task GetResponseAsync_WithRealAPI_ReturnsValidResponse()
    {
        Skip.If(string.IsNullOrEmpty(_apiKey), "Missing API Key");
        // Arrange
        var httpClient = new HttpClient();
        var endpoint = _configuration["Mistral:Endpoint"] ?? "https://api.mistral.ai/v1/chat/completions";
        var client = new MistralChatClient(httpClient, new Uri(endpoint))
        {
            OnBeforeRequest = req => req.Headers.Add("Authorization", $"Bearer {_apiKey}")
        };

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Explain quantum entanglement in one sentence.")
        };

        // Act
        var response = await client.GetResponseAsync(messages);

        // Assert
        response.Should().NotBeNull();
        response.Messages.Should().NotBeEmpty();
        response.Messages[0].Text.Should().NotBeNullOrEmpty();
        
        Console.WriteLine($"Response: {response.Messages[0].Text}");
    }

    [SkippableFact]
    public async Task GetStreamingResponseAsync_WithRealAPI_ReturnsValidStream()
    {
        Skip.If(string.IsNullOrEmpty(_apiKey), "Missing API Key");

        // Arrange
        var httpClient = new HttpClient();
        var endpoint = _configuration["Mistral:Endpoint"] ?? "https://api.mistral.ai/v1/chat/completions";
        var client = new MistralChatClient(httpClient, new Uri(endpoint))
        {
            OnBeforeRequest = req => req.Headers.Add("Authorization", $"Bearer {_apiKey}")
        };

        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Count from 1 to 5.")
        };

        // Act
        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in client.GetStreamingResponseAsync(messages))
        {
            updates.Add(update);
        }

        // Assert
        updates.Should().NotBeEmpty();
        var fullText = string.Concat(updates.Select(u => u.Text));
        fullText.Should().Contain("1");
        fullText.Should().Contain("5");
        
        Console.WriteLine($"Streaming Response: {fullText}");
    }
}
