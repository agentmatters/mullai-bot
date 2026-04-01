using Microsoft.Extensions.AI;
using Mullai.Providers.LLMProviders.Mistral;

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
            new(ChatRole.Tool, "result 1"),
            new(ChatRole.Tool, "result 2"),
            new(ChatRole.User, "Next question")
        };

        // Act
        var result = _consolidator.Consolidate(messages).ToList();

        // Assert
        // Expected: [Tool 1, Tool 2, Assistant (Empty), User]
        Assert.Equal(4, result.Count);
        Assert.Equal(ChatRole.Tool, result[0].Role);
        Assert.Equal(ChatRole.Tool, result[1].Role);
        Assert.Equal(ChatRole.Assistant, result[2].Role);
        Assert.Equal("\u200b", result[2].Text);
        Assert.Equal(ChatRole.User, result[3].Role);
    }

    [Fact]
    public void Consolidate_SingleToolMessage_InsertsAssistantAfterIt()
    {
        // Arrange
        var messages = new List<ChatMessage>
        {
            new(ChatRole.Tool, "result 1"),
            new(ChatRole.User, "Next question")
        };

        // Act
        var result = _consolidator.Consolidate(messages).ToList();

        // Assert
        // Expected: [Tool 1, Assistant (Empty), User]
        Assert.Equal(3, result.Count);
        Assert.Equal(ChatRole.Tool, result[0].Role);
        Assert.Equal(ChatRole.Assistant, result[1].Role);
        Assert.Equal(ChatRole.User, result[2].Role);
    }
}