using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.InteropServices;
using Mullai.Abstractions.Configuration;
using Mullai.Abstractions.Models;

namespace Mullai.Global.ServiceConfiguration;

public class MullaiConfigurationManager : IMullaiConfigurationManager
{
    private readonly string _configDir;
    private readonly string _credentialsPath;
    private readonly string _settingsPath;
    
    private Dictionary<string, string> _credentials = new();
    private MullaiAppSettings _settings = new();
    
    public event Action? OnConfigurationChanged;
    
    private static readonly byte[] Salt = Encoding.UTF8.GetBytes("MullaiSecureSalt");
    private const string EncryptionPrefix = "enc:";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public MullaiConfigurationManager()
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _configDir = Path.Combine(homeDir, ".mullai");
        _credentialsPath = Path.Combine(_configDir, "credentials.json");
        _settingsPath = Path.Combine(_configDir, "settings.json");

        Load();
    }

    // ICredentialStorage Implementation
    public string? GetApiKey(string providerName)
    {
        if (_credentials.TryGetValue(providerName, out var value))
        {
            return DecryptIfNeeded(value);
        }
        return null;
    }

    public void SaveApiKey(string providerName, string apiKey)
    {
        _credentials[providerName] = Encrypt(apiKey);
        SaveCredentials();
    }

    public void DeleteApiKey(string providerName)
    {
        if (_credentials.Remove(providerName))
        {
            SaveCredentials();
        }
    }

    public bool IsProviderEnabled(string providerName, bool defaultValue)
    {
        var key = GetProviderKey(providerName);
        if (_credentials.TryGetValue(key, out var enabledStr))
        {
            return bool.TryParse(enabledStr, out var enabled) ? enabled : defaultValue;
        }
        return defaultValue;
    }

    public void SetProviderEnabled(string providerName, bool enabled)
    {
        var key = GetProviderKey(providerName);
        _credentials[key] = enabled.ToString();
        SaveCredentials();
    }

    private string GetProviderKey(string providerName) => $"Provider:{providerName}:Enabled";

    public bool IsModelEnabled(string providerName, string modelId, bool defaultValue)
    {
        var key = GetModelKey(providerName, modelId);
        if (_credentials.TryGetValue(key, out var enabledStr))
        {
            return bool.TryParse(enabledStr, out var enabled) ? enabled : defaultValue;
        }
        return defaultValue;
    }

    public void SetModelEnabled(string providerName, string modelId, bool enabled)
    {
        var key = GetModelKey(providerName, modelId);
        _credentials[key] = enabled.ToString();
        SaveCredentials();
    }

    private string GetModelKey(string providerName, string modelId) => $"Model:{providerName}:{modelId}:Enabled";

    // IMullaiConfigurationManager Implementation
    public MullaiProvidersConfig GetProvidersConfig() => _settings.ProvidersConfig;
    
    public void SaveProvidersConfig(MullaiProvidersConfig config)
    {
        _settings.ProvidersConfig = config;
        SaveSettings();
    }
    
    public void AddModelDescriptor(string providerName, MullaiModelDescriptor model)
    {
        var provider = _settings.ProvidersConfig.Providers.FirstOrDefault(p => p.Name.Equals(providerName, StringComparison.OrdinalIgnoreCase));
        if (provider != null)
        {
            provider.Models.Add(model);
            SaveSettings();
        }
    }

    public List<CustomProviderDescriptor> GetCustomProviders() => _settings.CustomProviders;
    
    public void AddCustomProvider(CustomProviderDescriptor provider)
    {
        _settings.CustomProviders.RemoveAll(p => p.Name == provider.Name);
        _settings.CustomProviders.Add(provider);
        SaveSettings();
    }

    public void RemoveCustomProvider(string name)
    {
        if (_settings.CustomProviders.RemoveAll(p => p.Name == name) > 0)
        {
            SaveSettings();
        }
    }

    public SkillConfiguration GetSkillConfiguration() => _settings.Skills;
    
    public void SaveSkillConfiguration(SkillConfiguration configuration)
    {
        _settings.Skills = configuration;
        SaveSettings();
    }

    public McpConfiguration GetMcpConfiguration() => _settings.Mcp;
    
    public void SaveMcpConfiguration(McpConfiguration configuration)
    {
        _settings.Mcp = configuration;
        SaveSettings();
    }

    public void DeleteMcpServer(string serverName)
    {
        if (_settings.Mcp.Servers.RemoveAll(s => s.Name.Equals(serverName, StringComparison.OrdinalIgnoreCase)) > 0)
        {
            SaveSettings();
        }
    }

    private void Load()
    {
        if (!Directory.Exists(_configDir))
        {
            Directory.CreateDirectory(_configDir);
        }

        // Load Credentials
        if (File.Exists(_credentialsPath))
        {
            try
            {
                var json = File.ReadAllText(_credentialsPath);
                _credentials = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
                MigrateCredentials();
            }
            catch { _credentials = new(); }
        }

        // Load Settings
        if (File.Exists(_settingsPath))
        {
            try
            {
                var json = File.ReadAllText(_settingsPath);
                _settings = JsonSerializer.Deserialize<MullaiAppSettings>(json) ?? new();
            }
            catch { _settings = new(); }
        }
        else
        {
            // Try to migrate from old models.json if settings.json doesn't exist
            var oldModelsPath = Path.Combine(_configDir, "models.json");
            if (File.Exists(oldModelsPath))
            {
                try
                {
                    var json = File.ReadAllText(oldModelsPath);
                    _settings.ProvidersConfig = JsonSerializer.Deserialize<MullaiProvidersConfig>(json) ?? new();
                }
                catch { }
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
        bool migrated = false;
        foreach (var key in _credentials.Keys.ToList())
        {
            if (!key.StartsWith("Model:") && !key.StartsWith("Provider:") && !_credentials[key].StartsWith(EncryptionPrefix))
            {
                _credentials[key] = Encrypt(_credentials[key]);
                migrated = true;
            }
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
            {
                try
                {
                    System.Diagnostics.Process.Start("chmod", $"600 \"{path}\"")?.WaitForExit();
                }
                catch { }
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
        catch { return cipherText; }
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
}
