namespace Mullai.Abstractions.Configuration;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class McpConfigurationRequirementAttribute : Attribute
{
    public string Key { get; }
    public string Description { get; }
    public bool IsSecret { get; }
    public string? HelpUrl { get; set; }

    public McpConfigurationRequirementAttribute(string key, string description, bool isSecret = true)
    {
        Key = key;
        Description = description;
        IsSecret = isSecret;
    }
}

public class McpConfigurationRequirement
{
    public string Key { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSecret { get; set; } = true;
    public string? HelpUrl { get; set; }
}

