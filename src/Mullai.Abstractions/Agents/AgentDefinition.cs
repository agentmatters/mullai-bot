namespace Mullai.Abstractions.Agents;

public class AgentDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public List<string> Tools { get; set; } = new();
    public List<string> MemoryContexts { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}
