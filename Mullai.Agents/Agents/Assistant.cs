using Mullai.Abstractions.Agents;

namespace Mullai.Agents.Agents;


public class Assistant : IMullaiAgent
{
    public string Name { get; set; } = "Assistant";

    public string Instructions { get; set; } = "You are a helpful assistant that helps people find information.";
}