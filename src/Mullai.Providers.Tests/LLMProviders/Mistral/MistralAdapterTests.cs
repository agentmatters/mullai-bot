using System.Text.Json;
using Microsoft.Extensions.AI;
using Mullai.Providers.LLMProviders.Mistral;

namespace Mullai.Providers.Tests.LLMProviders.Mistral;

public class MistralAdapterTests
{
    private readonly MistralAdapter _adapter = new();

    [Fact]
    public void MapRequest_BasicMessage_MapsCorrectRoleAndContent()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User, "Hello Mistral")
        };
        var options = new ChatOptions { ModelId = "mistral-large-latest" };

        // Act
        var request = _adapter.MapRequest(messages, options, false);

        // Assert
        Assert.NotNull(request);
        Assert.Equal("mistral-large-latest", request.Model);
        Assert.Single(request.Messages);
        Assert.Equal("user", request.Messages[0].Role);
        Assert.Equal("Hello Mistral", request.Messages[0].Content);
    }

    [Fact]
    public void MapRequest_WithPenalties_MapsCorrectValues()
    {
        // Arrange
        var messages = new List<ChatMessage> { new(ChatRole.User, "test") };
        var options = new ChatOptions
        {
            FrequencyPenalty = 0.5f,
            PresencePenalty = 0.8f,
            Temperature = 0.7f,
            MaxOutputTokens = 100
        };

        // Act
        var request = _adapter.MapRequest(messages, options, false);

        // Assert
        Assert.Equal(0.5f, request.FrequencyPenalty);
        Assert.Equal(0.8f, request.PresencePenalty);
        Assert.Equal(0.7f, request.Temperature);
        Assert.Equal(100, request.MaxTokens);
    }

    [Fact]
    public void MapRequest_JsonSchema_MapsCorrectResponseFormat()
    {
        // Arrange
        var messages = new List<ChatMessage> { new(ChatRole.User, "test") };
        var schema = @"{ ""type"": ""object"", ""properties"": { ""name"": { ""type"": ""string"" } } }";
        var options = new ChatOptions
        {
            ResponseFormat = new ChatResponseFormatJson(JsonDocument.Parse(schema).RootElement)
        };

        // Act
        var request = _adapter.MapRequest(messages, options, false);

        // Assert
        Assert.NotNull(request.ResponseFormat);
        Assert.Equal(MistralResponseFormatType.JsonSchema, request.ResponseFormat!.Type);
        Assert.NotNull(request.ResponseFormat.JsonSchema);
        Assert.Equal("response", request.ResponseFormat.JsonSchema!.Name);
        Assert.True(request.ResponseFormat.JsonSchema.Strict);
    }

    [Fact]
    public void MapResponse_BasicResponse_MapsCorrectMessage()
    {
        // Arrange
        var responseDto = new MistralChatResponse
        {
            Id = "test-id",
            Model = "mistral-small",
            Choices = new List<MistralChoice>
            {
                new() { Message = new MistralChatMessage("assistant", "Hello human") }
            },
            Usage = new MistralUsage { PromptTokens = 10, CompletionTokens = 20, TotalTokens = 30 }
        };

        // Act
        var result = _adapter.MapResponse(responseDto);

        // Assert
        Assert.Single(result.Messages);
        Assert.Equal(ChatRole.Assistant, result.Messages[0].Role);
        Assert.Equal("Hello human", result.Messages[0].Text);
        Assert.Equal("test-id", result.ResponseId);
        Assert.Equal(10, result.Usage!.InputTokenCount);
        Assert.Equal(20, result.Usage.OutputTokenCount);
        Assert.Equal(30, result.Usage.TotalTokenCount);
    }

    [Fact]
    public void MapStreamingUpdate_TextDelta_MapsCorrectContent()
    {
        // Arrange
        var updateDto = new MistralStreamingUpdate
        {
            Id = "stream-id",
            Choices = new List<MistralStreamingChoice>
            {
                new() { Delta = new MistralDelta { Content = "Hello ", Role = "assistant" } }
            }
        };

        // Act
        var result = _adapter.MapStreamingUpdate(updateDto);

        // Assert
        Assert.Single(result.Contents);
        Assert.Equal("Hello ", result.Text);
        Assert.Equal(ChatRole.Assistant, result.Role);
        Assert.Equal("stream-id", result.ResponseId);
    }

    [Fact]
    public void MapRequest_MultipleToolResults_MapsToMultipleMistralMessages()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new(ChatRole.Tool, (string?)null)
            {
                Contents =
                {
                    new FunctionResultContent("call_1", "result_1"),
                    new FunctionResultContent("call_2", "result_2")
                }
            }
        };
        var options = new ChatOptions { ModelId = "mistral-large-latest" };

        // Act
        var request = _adapter.MapRequest(messages, options, false);

        // Assert
        Assert.Equal(2, request.Messages.Count);
        Assert.Equal("tool", request.Messages[0].Role);
        Assert.Equal("call_1", request.Messages[0].ToolCallId);
        Assert.Equal("result_1", request.Messages[0].Content);

        Assert.Equal("tool", request.Messages[1].Role);
        Assert.Equal("call_2", request.Messages[1].ToolCallId);
        Assert.Equal("result_2", request.Messages[1].Content);
    }
}