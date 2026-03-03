using Mullai.Abstractions.Agents;

namespace Mullai.Agents.Agents;

public class Joker : IMullaiAgent
{
    public string Name { get; set; } = "Joker";

    public string Instructions { get; set; } = "You are good at telling jokes.";
}