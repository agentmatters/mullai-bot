namespace Mullai.Providers.Models;

public class MullaiModelDescriptor
{
    /// <summary>The model identifier used in API calls (e.g. "gemini-2.5-flash").</summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>Human-readable display name.</summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>Lower number = tried first within the same provider.</summary>
    public int Priority { get; set; } = 1;

    /// <summary>When false the model is excluded from the active client list.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>e.g. ["chat", "vision", "tool_use", "embedding"]</summary>
    public List<string> Capabilities { get; set; } = [];

    public ModelPricing? Pricing { get; set; }

    /// <summary>Maximum context window in tokens.</summary>
    public int ContextWindow { get; set; }

    /// <summary>Any additional provider-specific or application-specific metadata.</summary>
    public Dictionary<string, string> Metadata { get; set; } = [];
}
