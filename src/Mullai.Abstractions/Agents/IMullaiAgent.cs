namespace Mullai.Abstractions.Agents;

public interface IMullaiAgent
{
    public string Name { get; set; }
    public string Instructions { get; set; }
}