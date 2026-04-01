using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using Mullai.Providers.Common.Http;
using Mullai.Providers.Common.Models;

namespace Mullai.Providers.LLMProviders.Mistral;

/// <summary>
///     Adapts Mistral-specific Request/Response DTOs.
/// </summary>
public class MistralAdapter : IProviderAdapter<MistralChatRequest, MistralChatResponse, MistralStreamingUpdate>
{
    public MistralChatRequest MapRequest(IEnumerable<ChatMessage> messages, ChatOptions? options, bool isStreaming)
    {
        var request = new MistralChatRequest
        {
            Model = options?.ModelId ?? "mistral-medium-latest",
            Messages = messages.SelectMany(m => MapToMistralMessages(m)).ToList(),
            Temperature = options?.Temperature,
            TopP = options?.TopP,
            MaxTokens = options?.MaxOutputTokens,
            FrequencyPenalty = options?.FrequencyPenalty,
            PresencePenalty = options?.PresencePenalty,
            RandomSeed = (int?)options?.Seed,
            Stop = options?.StopSequences?.Count > 0 ? options.StopSequences : null,
            Stream = isStreaming
        };

        if (options?.ResponseFormat is ChatResponseFormatJson jsonFormat)
        {
            if (jsonFormat.Schema != null)
                request.ResponseFormat = new MistralResponseFormat
                {
                    Type = MistralResponseFormatType.JsonSchema,
                    JsonSchema = new MistralJsonSchema
                    {
                        Name = "response", // Default name as required by API
                        SchemaDefinition = JsonNode.Parse(jsonFormat.Schema.Value.GetRawText())?.AsObject() ??
                                           new JsonObject(),
                        Strict = true
                    }
                };
            else
                request.ResponseFormat = new MistralResponseFormat { Type = MistralResponseFormatType.JsonObject };
        }
        else if (options?.ResponseFormat is ChatResponseFormatText)
        {
            request.ResponseFormat = new MistralResponseFormat { Type = MistralResponseFormatType.Text };
        }

        if (options?.Tools?.Count > 0)
        {
            request.Tools = options.Tools.OfType<AIFunctionDeclaration>().Select(f => new MistralTool
            {
                Function = new MistralFunction
                {
                    Name = f.Name,
                    Description = f.Description,
                    Parameters = f.JsonSchema.Deserialize<JsonObject>() ?? new JsonObject()
                }
            }).ToList();

            if (options.ToolMode is RequiredChatToolMode)
                request.ToolChoice = "required";
            else if (options.ToolMode is AutoChatToolMode or null)
                request.ToolChoice = "auto";
            else if (options.ToolMode is NoneChatToolMode) request.ToolChoice = "none";

            request.ParallelToolCalls = options.AllowMultipleToolCalls;
        }

        return request;
    }

    public ChatResponse MapResponse(MistralChatResponse response)
    {
        var firstChoice = response.Choices.FirstOrDefault();
        var mistralMsg = firstChoice?.Message;

        var message = new ChatMessage(
            mistralMsg?.Role == "assistant" ? ChatRole.Assistant : ChatRole.User,
            mistralMsg?.Content);

        if (mistralMsg?.ToolCalls?.Count > 0)
            foreach (var tc in mistralMsg.ToolCalls)
            {
                var arguments = tc.Function.Arguments switch
                {
                    string s when !string.IsNullOrWhiteSpace(s) => JsonSerializer
                        .Deserialize<Dictionary<string, object?>>(s),
                    JsonElement e => e.Deserialize<Dictionary<string, object?>>(),
                    _ => tc.Function.Arguments as Dictionary<string, object?>
                };

                message.Contents.Add(new FunctionCallContent(tc.Id, tc.Function.Name, arguments));
            }

        return new ChatResponse(message)
        {
            ResponseId = response.Id,
            ModelId = response.Model,
            Usage = response.Usage != null
                ? new UsageDetails
                {
                    InputTokenCount = response.Usage.PromptTokens,
                    OutputTokenCount = response.Usage.CompletionTokens,
                    TotalTokenCount = response.Usage.TotalTokens
                }
                : null
        };
    }

    public ChatResponseUpdate MapStreamingUpdate(MistralStreamingUpdate streamUpdate)
    {
        var firstChoice = streamUpdate.Choices.FirstOrDefault();
        var delta = firstChoice?.Delta;

        var role = delta?.Role switch
        {
            "assistant" => ChatRole.Assistant,
            "system" => ChatRole.System,
            _ => (ChatRole?)null
        };

        var update = new ExtendedChatResponseUpdate
        {
            Role = role,
            ResponseId = streamUpdate.Id
        };

        if (delta?.Content != null) update.Contents.Add(new TextContent(delta.Content));

        if (delta?.ToolCalls?.Count > 0)
            foreach (var tc in delta.ToolCalls)
            {
                var arguments = tc.Function.Arguments switch
                {
                    string s when !string.IsNullOrWhiteSpace(s) => JsonSerializer
                        .Deserialize<Dictionary<string, object?>>(s),

                    JsonElement e when e.ValueKind == JsonValueKind.String &&
                                       !string.IsNullOrWhiteSpace(e.GetString()) =>
                        JsonSerializer.Deserialize<Dictionary<string, object?>>(e.GetString()!),

                    JsonElement e when e.ValueKind == JsonValueKind.Object =>
                        e.Deserialize<Dictionary<string, object?>>(),

                    _ => tc.Function.Arguments as Dictionary<string, object?>
                };


                update.Contents.Add(new FunctionCallContent(tc.Id, tc.Function.Name, arguments));
                // Note: Standard Microsoft.Extensions.AI streaming of tool calls often involves accumulation
            }

        if (streamUpdate.Usage != null)
            update.Contents.Add(new UsageContent(new UsageDetails
            {
                InputTokenCount = streamUpdate.Usage.PromptTokens,
                OutputTokenCount = streamUpdate.Usage.CompletionTokens,
                TotalTokenCount = streamUpdate.Usage.TotalTokens
            }));

        return update;
    }

    private IEnumerable<MistralChatMessage> MapToMistralMessages(ChatMessage m)
    {
        var role = m.Role.ToString().ToLowerInvariant();

        if (m.Role == ChatRole.Tool)
        {
            // For tool messages, Mistral expects one message per tool result.
            // Each contains ToolCallId and Content (result)
            foreach (var toolResult in m.Contents.OfType<FunctionResultContent>())
                yield return new MistralChatMessage(role, toolResult.Result?.ToString())
                {
                    ToolCallId = toolResult.CallId
                };
        }
        else if (m.Role == ChatRole.Assistant)
        {
            var assistantMsg = new MistralChatMessage(role, m.Text);

            // For assistant messages, they might contain tool calls
            var toolCalls = m.Contents.OfType<FunctionCallContent>().ToList();
            if (toolCalls.Any())
                assistantMsg.ToolCalls = toolCalls.Select(tc => new MistralToolCall
                {
                    Id = tc.CallId,
                    Function = new MistralFunctionCall
                    {
                        Name = tc.Name,
                        Arguments = tc.Arguments ?? new Dictionary<string, object?>()
                    }
                }).ToList();

            yield return assistantMsg;
        }
        else
        {
            yield return new MistralChatMessage(role, m.Text);
        }
    }
}