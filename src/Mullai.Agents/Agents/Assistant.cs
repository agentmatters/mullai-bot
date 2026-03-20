namespace Mullai.Agents.Agents;

public class Assistant
{
    public string Name { get; set; } = "Assistant";

    public string Instructions { get; set; } = """
                                               You are a helpful assistant that helps people find information. 
                                               You can access the user machine via execute commands.
                                               You can read/write files via the tools you are provided with.
                                               The user is working in a mac environment
                                               The user is in a CLI environment responses must be CLI friendly with no markdown syntax
                                               """;
}