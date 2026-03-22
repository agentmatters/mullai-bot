using Mullai.Abstractions.Models;

namespace Mullai.Abstractions.Configuration;

public interface IMullaiConfigurationManager : ICredentialStorage
{
    event Action? OnConfigurationChanged;
    MullaiProvidersConfig GetProvidersConfig();
    void SaveProvidersConfig(MullaiProvidersConfig config);
    void AddModelDescriptor(string providerName, MullaiModelDescriptor model);
    
    List<CustomProviderDescriptor> GetCustomProviders();
    void AddCustomProvider(CustomProviderDescriptor provider);
    void RemoveCustomProvider(string name);
    
    SkillConfiguration GetSkillConfiguration();
    void SaveSkillConfiguration(SkillConfiguration configuration);
    
    McpConfiguration GetMcpConfiguration();
    void SaveMcpConfiguration(McpConfiguration configuration);
}

public class CustomProviderDescriptor
{
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public List<string> Models { get; set; } = [];
    public bool Enabled { get; set; } = true;
}

public class SkillConfiguration
{
    public Dictionary<string, bool> EnabledSkills { get; set; } = [];
}

public class McpConfiguration
{
    public List<McpServerDescriptor> Servers { get; set; } = [];
}

public class McpServerDescriptor
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "stdio"; // stdio or sse
    public string Command { get; set; } = string.Empty;
    public string[] Args { get; set; } = [];
    public string Url { get; set; } = string.Empty; // for sse
    public bool Enabled { get; set; } = true;
}
