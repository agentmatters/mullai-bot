using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Mullai.Abstractions.Configuration;
using Mullai.Abstractions.Models;

namespace Mullai.Global.ServiceConfiguration;

public class MullaiConfigurationManager : IMullaiConfigurationManager
{
    private const string EncryptionPrefix = "enc:";

    private static readonly byte[] Salt = Encoding.UTF8.GetBytes("MullaiSecureSalt");
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly IBuiltInMcpProvider? _builtInMcpProvider;
    private readonly string _configDir;
    private readonly string _credentialsPath;
    private readonly string _settingsPath;

    private Dictionary<string, string> _credentials = new();
    private MullaiAppSettings _settings = new();

    public MullaiConfigurationManager(IBuiltInMcpProvider? builtInMcpProvider = null)
    {
        _builtInMcpProvider = builtInMcpProvider;
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _configDir = Path.Combine(homeDir, ".mullai");
        _credentialsPath = Path.Combine(_configDir, "credentials.json");
        _settingsPath = Path.Combine(_configDir, "settings.json");

        Load();
    }

    public event Action? OnConfigurationChanged;


    // ICredentialStorage Implementation
    public string? GetApiKey(string providerName)
    {
        if (_credentials.TryGetValue(providerName, out var value)) return DecryptIfNeeded(value);
        return null;
    }

    public void SaveApiKey(string providerName, string apiKey)
    {
        _credentials[providerName] = Encrypt(apiKey);
        SaveCredentials();
    }

    public void DeleteApiKey(string providerName)
    {
        if (_credentials.Remove(providerName)) SaveCredentials();
    }

    public bool IsProviderEnabled(string providerName, bool defaultValue)
    {
        var key = GetProviderKey(providerName);
        if (_credentials.TryGetValue(key, out var enabledStr))
            return bool.TryParse(enabledStr, out var enabled) ? enabled : defaultValue;
        return defaultValue;
    }

    public void SetProviderEnabled(string providerName, bool enabled)
    {
        var key = GetProviderKey(providerName);
        _credentials[key] = enabled.ToString();
        SaveCredentials();
    }

    public bool IsModelEnabled(string providerName, string modelId, bool defaultValue)
    {
        var key = GetModelKey(providerName, modelId);
        if (_credentials.TryGetValue(key, out var enabledStr))
            return bool.TryParse(enabledStr, out var enabled) ? enabled : defaultValue;
        return defaultValue;
    }

    public void SetModelEnabled(string providerName, string modelId, bool enabled)
    {
        var key = GetModelKey(providerName, modelId);
        _credentials[key] = enabled.ToString();
        SaveCredentials();
    }

    // IMullaiConfigurationManager Implementation
    public MullaiProvidersConfig GetProvidersConfig()
    {
        return _settings.ProvidersConfig;
    }

    public void SaveProvidersConfig(MullaiProvidersConfig config)
    {
        _settings.ProvidersConfig = config;
        SaveSettings();
    }

    public void AddModelDescriptor(string providerName, MullaiModelDescriptor model)
    {
        var provider =
            _settings.ProvidersConfig.Providers.FirstOrDefault(p =>
                p.Name.Equals(providerName, StringComparison.OrdinalIgnoreCase));
        if (provider != null)
        {
            provider.Models.Add(model);
            SaveSettings();
        }
    }

    public List<CustomProviderDescriptor> GetCustomProviders()
    {
        return _settings.CustomProviders;
    }

    public void AddCustomProvider(CustomProviderDescriptor provider)
    {
        _settings.CustomProviders.RemoveAll(p => p.Name == provider.Name);
        _settings.CustomProviders.Add(provider);
        SaveSettings();
    }

    public void RemoveCustomProvider(string name)
    {
        if (_settings.CustomProviders.RemoveAll(p => p.Name == name) > 0) SaveSettings();
    }

    public SkillConfiguration GetSkillConfiguration()
    {
        return _settings.Skills;
    }

    public void SaveSkillConfiguration(SkillConfiguration configuration)
    {
        _settings.Skills = configuration;
        SaveSettings();
    }

    public McpConfiguration GetMcpConfiguration()
    {
        var config = _settings.Mcp;

        if (_builtInMcpProvider != null)
        {
            var builtInServers = _builtInMcpProvider.GetBuiltInServers();
            foreach (var builtIn in builtInServers)
            {
                var existing =
                    config.Servers.FirstOrDefault(s => s.Name.Equals(builtIn.Name, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    existing.IsBuiltIn = true;
                    // Keep existing Enabled state from user config
                    // but ensure Command/Args are from built-in definition
                    existing.Command = builtIn.Command;
                    existing.Args = builtIn.Args;
                    existing.Type = builtIn.Type;
                }
                else
                {
                    config.Servers.Add(builtIn);
                }
            }
        }

        return config;
    }

    public void SaveMcpConfiguration(McpConfiguration configuration)
    {
        _settings.Mcp = configuration;
        SaveSettings();
    }

    public void DeleteMcpServer(string serverName)
    {
        var server =
            _settings.Mcp.Servers.FirstOrDefault(s => s.Name.Equals(serverName, StringComparison.OrdinalIgnoreCase));
        if (server != null && server.IsBuiltIn)
            // Do not delete built-in servers from the configuration list,
            // but we can allow them to be disabled (which is handled by SaveMcpConfiguration).
            return;

        if (_settings.Mcp.Servers.RemoveAll(s => s.Name.Equals(serverName, StringComparison.OrdinalIgnoreCase)) >
            0) SaveSettings();
    }

    public string? GetMcpSecret(string key)
    {
        var secretKey = $"McpSecret:{key}";
        if (_credentials.TryGetValue(secretKey, out var value)) return DecryptIfNeeded(value);
        return null;
    }

    public void SaveMcpSecret(string key, string value)
    {
        var secretKey = $"McpSecret:{key}";
        _credentials[secretKey] = Encrypt(value);
        SaveCredentials();
    }

    public List<AgentDefinition> GetAgents()
    {
        if (_settings.Agents.Count == 0)
        {
            _settings.Agents = GetDefaultAgents();
            SaveSettings();
        }

        return _settings.Agents;
    }

    public void SaveAgent(AgentDefinition agent)
    {
        _settings.Agents.RemoveAll(a => a.Id == agent.Id);
        _settings.Agents.Add(agent);
        SaveSettings();
    }

    public void DeleteAgent(string agentId)
    {
        if (_settings.Agents.RemoveAll(a => a.Id == agentId) > 0) SaveSettings();
    }

    public List<string> GetAllAvailableToolGroups()
    {
        var tools = new List<string>
        {
            "FileSystemTool",
            "BashTool",
            "WeatherTool",
            "CliTool",
            "TodoTool",
            "WebTool",
            "CodeSearchTool",
            "WorkflowTool",
            "WorkflowStateTool",
            "RestApiTool",
            "HtmlToMarkdownTool",
            "DynamicToolLoader"
        };

        var mcpConfig = GetMcpConfiguration();
        foreach (var server in mcpConfig.Servers)
            if (server.Enabled)
                tools.Add($"MCP:{server.Name}");

        return tools;
    }

    private string GetProviderKey(string providerName)
    {
        return $"Provider:{providerName}:Enabled";
    }

    private string GetModelKey(string providerName, string modelId)
    {
        return $"Model:{providerName}:{modelId}:Enabled";
    }

    private List<AgentDefinition> GetDefaultAgents()
    {
        return new List<AgentDefinition>
        {
            new()
            {
                Id = "assistant",
                Name = "Assistant",
                Instructions = """
                               You are a helpful assistant that helps people find information. 
                               You can access the user machine via execute commands.
                               You can read/write files via the tools you are provided with.
                               The user is working in a mac environment.
                               The user is in a CLI environment responses must be CLI friendly with no markdown syntax.
                               IMPORTANT: You have a DynamicToolLoader available. If you need a tool (like Weather, WebSearch, Todo etc) that is not currently in your loaded tools list, you MUST call `GetAvailableTools` to discover what tool groups exist, and then call `LoadToolGroup` to load the exact tool group you need. After loading it, you can use the newly loaded tools in your subsequent responses.
                               """,
                Tools = new List<AgentToolDefinition>
                {
                    new() { Name = "FileSystemTool", IsDefault = true },
                    new() { Name = "BashTool", IsDefault = true },
                    new() { Name = "DynamicToolLoader", IsDefault = true }
                }
            }
        };
    }


    private void Load()
    {
        if (!Directory.Exists(_configDir)) Directory.CreateDirectory(_configDir);

        // Load Credentials
        if (File.Exists(_credentialsPath))
            try
            {
                var json = File.ReadAllText(_credentialsPath);
                _credentials = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ??
                               new Dictionary<string, string>();
                MigrateCredentials();
            }
            catch
            {
                _credentials = new Dictionary<string, string>();
            }

        // Load Settings
        if (File.Exists(_settingsPath))
        {
            try
            {
                var json = File.ReadAllText(_settingsPath);
                _settings = JsonSerializer.Deserialize<MullaiAppSettings>(json) ?? new MullaiAppSettings();
            }
            catch
            {
                _settings = new MullaiAppSettings();
            }
        }
        else
        {
            // Try to migrate from old models.json if settings.json doesn't exist
            var oldModelsPath = Path.Combine(_configDir, "models.json");
            if (File.Exists(oldModelsPath))
                try
                {
                    var json = File.ReadAllText(oldModelsPath);
                    _settings.ProvidersConfig = JsonSerializer.Deserialize<MullaiProvidersConfig>(json) ??
                                                new MullaiProvidersConfig();
                }
                catch
                {
                }
        }

        // Initialize defaults if empty
        if (_settings.ProvidersConfig.Providers.Count == 0)
        {
            _settings.ProvidersConfig.Providers = GetDefaultProviders();
            SaveSettings();
        }
    }

    private List<MullaiProviderDescriptor> GetDefaultProviders()
    {
        return new List<MullaiProviderDescriptor>
        {
            new() { Name = "OpenRouter", Priority = 1, Enabled = true },
            new() { Name = "Gemini", Priority = 2, Enabled = true },
            new() { Name = "Groq", Priority = 3, Enabled = true },
            new() { Name = "Mistral", Priority = 4, Enabled = true },
            new() { Name = "Cerebras", Priority = 5, Enabled = true },
            new() { Name = "OllamaOpenAI", Priority = 6, Enabled = true },
            new() { Name = "Nvidia", Priority = 7, Enabled = true }
        };
    }

    private void MigrateCredentials()
    {
        var migrated = false;
        foreach (var key in _credentials.Keys.ToList())
            if (!key.StartsWith("Model:") && !key.StartsWith("Provider:") &&
                !_credentials[key].StartsWith(EncryptionPrefix))
            {
                _credentials[key] = Encrypt(_credentials[key]);
                migrated = true;
            }

        if (migrated) SaveCredentials();
    }

    private void SaveCredentials()
    {
        SafeWriteFile(_credentialsPath, JsonSerializer.Serialize(_credentials, JsonOptions));
        OnConfigurationChanged?.Invoke();
    }

    private void SaveSettings()
    {
        SafeWriteFile(_settingsPath, JsonSerializer.Serialize(_settings, JsonOptions));
        OnConfigurationChanged?.Invoke();
    }

    private void SafeWriteFile(string path, string content)
    {
        try
        {
            File.WriteAllText(path, content);
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                try
                {
                    Process.Start("chmod", $"600 \"{path}\"")?.WaitForExit();
                }
                catch
                {
                }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving config to {path}: {ex.Message}");
        }
    }

    private string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;
        using var aes = Aes.Create();
        var key = GetEncryptionKey();
        aes.Key = key;
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length);
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        return EncryptionPrefix + Convert.ToBase64String(ms.ToArray());
    }

    private string DecryptIfNeeded(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText) || !cipherText.StartsWith(EncryptionPrefix)) return cipherText;
        try
        {
            var base64 = cipherText.Substring(EncryptionPrefix.Length);
            var fullCipher = Convert.FromBase64String(base64);
            using var aes = Aes.Create();
            var key = GetEncryptionKey();
            aes.Key = key;
            var iv = new byte[aes.BlockSize / 8];
            Array.Copy(fullCipher, 0, iv, 0, iv.Length);
            aes.IV = iv;
            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            return sr.ReadToEnd();
        }
        catch
        {
            return cipherText;
        }
    }

    private byte[] GetEncryptionKey()
    {
        var machineId = Environment.MachineName + Environment.UserName;
        return Rfc2898DeriveBytes.Pbkdf2(machineId, Salt, 10000, HashAlgorithmName.SHA256, 32);
    }
}

internal class MullaiAppSettings
{
    public MullaiProvidersConfig ProvidersConfig { get; set; } = new();
    public List<CustomProviderDescriptor> CustomProviders { get; set; } = [];
    public SkillConfiguration Skills { get; set; } = new();
    public McpConfiguration Mcp { get; set; } = new();
    public List<AgentDefinition> Agents { get; set; } = [];
}