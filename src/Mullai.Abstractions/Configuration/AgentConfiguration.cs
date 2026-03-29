namespace Mullai.Abstractions.Configuration;

public class AgentDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public List<AgentToolDefinition> Tools { get; set; } = [];
    public bool Enabled { get; set; } = true;
}

public class AgentToolDefinition
{
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; } // true = default loaded, false = loaded on demand
}
