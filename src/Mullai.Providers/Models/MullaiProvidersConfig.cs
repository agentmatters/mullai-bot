namespace Mullai.Providers.Models;

/// <summary>Root object that maps from models.json → strongly-typed config.</summary>
public class MullaiProvidersConfig
{
    public List<MullaiProviderDescriptor> Providers { get; set; } = [];
}
