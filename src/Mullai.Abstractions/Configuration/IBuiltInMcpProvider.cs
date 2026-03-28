namespace Mullai.Abstractions.Configuration;

public interface IBuiltInMcpProvider
{
    IEnumerable<McpServerDescriptor> GetBuiltInServers();
}
