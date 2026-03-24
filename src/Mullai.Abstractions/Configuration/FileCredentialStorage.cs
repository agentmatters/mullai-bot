using System.Text.Json;
using System.Runtime.InteropServices;
using Mullai.Abstractions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace Mullai.Abstractions.Configuration;

public class FileCredentialStorage : ICredentialStorage
{
    private readonly string _filePath;
    private Dictionary<string, string> _credentials = new();
    private static readonly byte[] Salt = Encoding.UTF8.GetBytes("MullaiSecureSalt");
    private const string EncryptionPrefix = "enc:";

    public FileCredentialStorage()
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var configDir = Path.Combine(homeDir, ".mullai");
        _filePath = Path.Combine(configDir, "credentials.json");

        Load();
    }

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
        Save();
    }

    public void DeleteApiKey(string providerName)
    {
        if (_credentials.Remove(providerName))
        {
            Save();
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
        Save();
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
        Save();
    }

    private string GetModelKey(string providerName, string modelId) => $"Model:{providerName}:{modelId}:Enabled";

    private void Load()
    {
        if (!File.Exists(_filePath)) return;

        try
        {
            var json = File.ReadAllText(_filePath);
            _credentials = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
            
            // Migrate any plain text keys to encrypted format
            bool migrated = false;
            foreach (var key in _credentials.Keys.ToList())
            {
                if (!key.StartsWith("Model:") && !key.StartsWith("Provider:") && !_credentials[key].StartsWith(EncryptionPrefix))
                {
                    _credentials[key] = Encrypt(_credentials[key]);
                    migrated = true;
                }
            }
            if (migrated) Save();
        }
        catch
        {
            _credentials = new();
        }
    }

    private void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(_credentials, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);

            // Set restricted permissions on Unix-like systems
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    var process = System.Diagnostics.Process.Start("chmod", $"600 \"{_filePath}\"");
                    process.WaitForExit();
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving credentials: {ex.Message}");
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
        ms.Write(aes.IV, 0, aes.IV.Length); // Prepend IV
        
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
