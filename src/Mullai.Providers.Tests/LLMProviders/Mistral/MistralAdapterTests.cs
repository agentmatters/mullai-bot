using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Mullai.Providers.LLMProviders.Mistral;
using Xunit;

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
        var request = _adapter.MapRequest(messages, options, isStreaming: false);

        // Assert
        request.Should().NotBeNull();
        request.Model.Should().Be("mistral-large-latest");
        request.Messages.Should().HaveCount(1);
        request.Messages[0].Role.Should().Be("user");
        request.Messages[0].Content.Should().Be("Hello Mistral");
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
        var request = _adapter.MapRequest(messages, options, isStreaming: false);

        // Assert
        request.FrequencyPenalty.Should().Be(0.5f);
        request.PresencePenalty.Should().Be(0.8f);
        request.Temperature.Should().Be(0.7f);
        request.MaxTokens.Should().Be(100);
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
        var request = _adapter.MapRequest(messages, options, isStreaming: false);

        // Assert
        request.ResponseFormat.Should().NotBeNull();
        request.ResponseFormat!.Type.Should().Be(MistralResponseFormatType.JsonSchema);
        request.ResponseFormat.JsonSchema.Should().NotBeNull();
        request.ResponseFormat.JsonSchema!.Name.Should().Be("response");
        request.ResponseFormat.JsonSchema.Strict.Should().BeTrue();
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
        result.Messages.Should().HaveCount(1);
        result.Messages[0].Role.Should().Be(ChatRole.Assistant);
        result.Messages[0].Text.Should().Be("Hello human");
        result.ResponseId.Should().Be("test-id");
        result.Usage!.InputTokenCount.Should().Be(10);
        result.Usage.OutputTokenCount.Should().Be(20);
        result.Usage.TotalTokenCount.Should().Be(30);
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
        result.Contents.Should().HaveCount(1);
        result.Text.Should().Be("Hello ");
        result.Role.Should().Be(ChatRole.Assistant);
        result.ResponseId.Should().Be("stream-id");
    }

    [Fact]
    public void MapRequest_MultipleToolResults_MapsToMultipleMistralMessages()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.Tool, (string?)null)
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
        var request = _adapter.MapRequest(messages, options, isStreaming: false);

        // Assert
        request.Messages.Should().HaveCount(2);
        request.Messages[0].Role.Should().Be("tool");
        request.Messages[0].ToolCallId.Should().Be("call_1");
        request.Messages[0].Content.Should().Be("result_1");
        
        request.Messages[1].Role.Should().Be("tool");
        request.Messages[1].ToolCallId.Should().Be("call_2");
        request.Messages[1].Content.Should().Be("result_2");
    }
}
