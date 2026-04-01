using System.Text;
using Microsoft.Extensions.AI;
using Mullai.Providers.Common.Http;

namespace Mullai.Providers.LLMProviders.Mistral;

/// <summary>
///     Consolidates messages for Mistral to ensure they follow Mistral's API rules.
/// </summary>
public class MistralConsolidator : IMessageConsolidator
{
    private const string EmptyMessage = "\u200b";

    public IEnumerable<ChatMessage> Consolidate(IEnumerable<ChatMessage> messages)
    {
        var inputList = messages.ToList();
        if (inputList.Count == 0) return inputList;

        // Mistral rejects 'name' (AuthorName) property.
        foreach (var m in inputList) m.AuthorName = null;

        var result = new List<ChatMessage>();

        // 1. Move System messages to the front and consolidate them.
        var systemMessages = inputList.Where(m => m.Role == ChatRole.System).ToList();
        if (systemMessages.Any())
        {
            var combinedSystemText = string.Join("\n", systemMessages.Select(m => m.Text));
            result.Add(new ChatMessage(ChatRole.System, combinedSystemText));
        }

        var otherMessages = inputList.Where(m => m.Role != ChatRole.System).ToList();

        // 2. Consolidate consecutive User messages.
        for (var i = 0; i < otherMessages.Count; i++)
        {
            var current = otherMessages[i];

            if (current.Role == ChatRole.User)
            {
                var combinedUserText = new StringBuilder(current.Text);
                while (i + 1 < otherMessages.Count && otherMessages[i + 1].Role == ChatRole.User)
                {
                    combinedUserText.Append("\n").Append(otherMessages[i + 1].Text);
                    i++;
                }

                result.Add(new ChatMessage(ChatRole.User, combinedUserText.ToString()));
            }
            else
            {
                result.Add(current);
            }
        }

        // 3. Ensure validation rules (similar to Mistral.SDK logic)
        // - Tool messages must be followed by an assistant message if there's anything next that isn't another tool message.
        for (var i = 0; i < result.Count; i++)
            if (result[i].Role == ChatRole.Tool && i + 1 < result.Count && result[i + 1].Role != ChatRole.Tool &&
                result[i + 1].Role != ChatRole.Assistant)
                result.Insert(i + 1, new ChatMessage(ChatRole.Assistant, EmptyMessage));

        // - Last message must not be Assistant.
        if (result.Count > 0 && result.Last().Role == ChatRole.Assistant)
            result.Add(new ChatMessage(ChatRole.User, EmptyMessage));

        return result;
    }
}