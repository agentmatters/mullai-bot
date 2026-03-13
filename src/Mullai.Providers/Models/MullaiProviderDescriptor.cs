namespace Mullai.Providers.Models;

public class MullaiProviderDescriptor
{
    /// <summary>Provider name — must match the factory key (e.g. "Gemini", "Groq").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Lower number = tried first across all providers.</summary>
    public int Priority { get; set; } = 1;

    /// <summary>When false the entire provider is excluded from the active client list.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Models available from this provider, ordered by Priority.</summary>
    public List<MullaiModelDescriptor> Models { get; set; } = [];
}
