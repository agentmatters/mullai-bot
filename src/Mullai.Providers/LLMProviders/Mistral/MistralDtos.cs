using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Mullai.Providers.LLMProviders.Mistral;

public class MistralChatRequest
{
    [JsonPropertyName("model")] public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")] public List<MistralChatMessage> Messages { get; set; } = new();

    [JsonPropertyName("temperature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? Temperature { get; set; }

    [JsonPropertyName("top_p")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? TopP { get; set; }

    [JsonPropertyName("max_tokens")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MaxTokens { get; set; }

    [JsonPropertyName("stream")] public bool Stream { get; set; }

    [JsonPropertyName("stop")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Stop { get; set; }

    [JsonPropertyName("random_seed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? RandomSeed { get; set; }

    [JsonPropertyName("response_format")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MistralResponseFormat? ResponseFormat { get; set; }

    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<MistralTool>? Tools { get; set; }

    [JsonPropertyName("tool_choice")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? ToolChoice { get; set; }

    [JsonPropertyName("presence_penalty")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? PresencePenalty { get; set; }

    [JsonPropertyName("frequency_penalty")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? FrequencyPenalty { get; set; }

    [JsonPropertyName("n")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? N { get; set; }

    [JsonPropertyName("safe_prompt")] public bool SafePrompt { get; set; }

    [JsonPropertyName("parallel_tool_calls")]
    public bool? ParallelToolCalls { get; set; } = true;

    [JsonPropertyName("prediction")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MistralPrediction? Prediction { get; set; }

    [JsonPropertyName("prompt_mode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PromptMode { get; set; }
}

public class MistralChatMessage
{
    public MistralChatMessage()
    {
    }

    public MistralChatMessage(string role, string? content)
    {
        Role = role;
        Content = content;
    }

    [JsonPropertyName("role")] public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")] public string? Content { get; set; }

    [JsonPropertyName("tool_calls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<MistralToolCall>? ToolCalls { get; set; }

    [JsonPropertyName("tool_call_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolCallId { get; set; }
}

public class MistralTool
{
    [JsonPropertyName("type")] public string Type { get; set; } = "function";

    [JsonPropertyName("function")] public MistralFunction Function { get; set; } = new();
}

public class MistralFunction
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;

    [JsonPropertyName("parameters")] public JsonObject Parameters { get; set; } = new();

    [JsonPropertyName("strict")] public bool Strict { get; set; }
}

public class MistralToolCall
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")] public string Type { get; set; } = "function";

    [JsonPropertyName("function")] public MistralFunctionCall Function { get; set; } = new();
}

public class MistralFunctionCall
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    [JsonPropertyName("arguments")] public object Arguments { get; set; } = string.Empty;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MistralResponseFormatType
{
    [JsonPropertyName("text")] Text,

    [JsonPropertyName("json_object")] JsonObject,

    [JsonPropertyName("json_schema")] JsonSchema
}

public class MistralResponseFormat
{
    [JsonPropertyName("type")] public MistralResponseFormatType Type { get; set; } = MistralResponseFormatType.Text;

    [JsonPropertyName("json_schema")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MistralJsonSchema? JsonSchema { get; set; }
}

public class MistralJsonSchema
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    [JsonPropertyName("schema_definition")]
    public JsonObject SchemaDefinition { get; set; } = new();

    [JsonPropertyName("strict")] public bool Strict { get; set; }
}

public class MistralPrediction
{
    [JsonPropertyName("type")] public string Type { get; set; } = "content";

    [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;
}

public class MistralChatResponse
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;

    [JsonPropertyName("object")] public string Object { get; set; } = string.Empty;

    [JsonPropertyName("created")] public long Created { get; set; }

    [JsonPropertyName("model")] public string Model { get; set; } = string.Empty;

    [JsonPropertyName("choices")] public List<MistralChoice> Choices { get; set; } = new();

    [JsonPropertyName("usage")] public MistralUsage? Usage { get; set; }
}

public class MistralChoice
{
    [JsonPropertyName("index")] public int Index { get; set; }

    [JsonPropertyName("message")] public MistralChatMessage Message { get; set; } = new();

    [JsonPropertyName("finish_reason")] public string? FinishReason { get; set; }
}

public class MistralStreamingUpdate
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;

    [JsonPropertyName("choices")] public List<MistralStreamingChoice> Choices { get; set; } = new();

    [JsonPropertyName("usage")] public MistralUsage? Usage { get; set; }
}

public class MistralStreamingChoice
{
    [JsonPropertyName("delta")] public MistralDelta Delta { get; set; } = new();

    [JsonPropertyName("finish_reason")] public string? FinishReason { get; set; }
}

public class MistralDelta
{
    [JsonPropertyName("role")] public string? Role { get; set; }

    [JsonPropertyName("content")] public string? Content { get; set; }

    [JsonPropertyName("tool_calls")] public List<MistralToolCall>? ToolCalls { get; set; }
}

public class MistralUsage
{
    [JsonPropertyName("prompt_tokens")] public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")] public int TotalTokens { get; set; }
}