namespace Mullai.Abstractions.Configuration;

public interface ICredentialStorage
{
    string? GetApiKey(string providerName);
    void SaveApiKey(string providerName, string apiKey);
    void DeleteApiKey(string providerName);
    
    bool IsProviderEnabled(string providerName, bool defaultValue);
    void SetProviderEnabled(string providerName, bool enabled);
    
    bool IsModelEnabled(string providerName, string modelId, bool defaultValue);
    void SetModelEnabled(string providerName, string modelId, bool enabled);
    void SetModelsEnabled(string providerName, IEnumerable<string> modelIds, bool enabled);
}
