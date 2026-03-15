using FluentAssertions;
using Microsoft.Extensions.AI;
using Mullai.Providers.LLMProviders.Mistral;
using Xunit;

namespace Mullai.Providers.Tests.LLMProviders.Mistral;

public class MistralConsolidatorTests
{
    private readonly MistralConsolidator _consolidator = new();

    [Fact]
    public void Consolidate_MultipleToolMessages_DoesNotInsertAssistantBetweenThem()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.Tool, "result 1"),
            new ChatMessage(ChatRole.Tool, "result 2"),
            new ChatMessage(ChatRole.User, "Next question")
        };

        // Act
        var result = _consolidator.Consolidate(messages).ToList();

        // Assert
        // Expected: [Tool 1, Tool 2, Assistant (Empty), User]
        result.Should().HaveCount(4);
        result[0].Role.Should().Be(ChatRole.Tool);
        result[1].Role.Should().Be(ChatRole.Tool);
        result[2].Role.Should().Be(ChatRole.Assistant);
        result[2].Text.Should().Be("\u200b");
        result[3].Role.Should().Be(ChatRole.User);
    }

    [Fact]
    public void Consolidate_SingleToolMessage_InsertsAssistantAfterIt()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.Tool, "result 1"),
            new ChatMessage(ChatRole.User, "Next question")
        };

        // Act
        var result = _consolidator.Consolidate(messages).ToList();

        // Assert
        // Expected: [Tool 1, Assistant (Empty), User]
        result.Should().HaveCount(3);
        result[0].Role.Should().Be(ChatRole.Tool);
        result[1].Role.Should().Be(ChatRole.Assistant);
        result[2].Role.Should().Be(ChatRole.User);
    }
}
