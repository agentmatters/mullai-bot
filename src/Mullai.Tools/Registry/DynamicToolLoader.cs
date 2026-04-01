using System.ComponentModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using Mullai.Abstractions.Configuration;

namespace Mullai.Tools.Registry;

[Description("A tool to discover and dynamically load other available tools into the current agent session.")]
public class DynamicToolLoader
{
    private readonly HashSet<string> _allowedDynamicTools = new(StringComparer.OrdinalIgnoreCase);
    private readonly IMullaiConfigurationManager _configManager;
    private readonly HashSet<string> _loadedToolGroups = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<DynamicToolLoader> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IList<AITool> _sessionTools;

    public DynamicToolLoader(
        IServiceProvider serviceProvider,
        IList<AITool> sessionTools,
        ILogger<DynamicToolLoader> logger,
        IMullaiConfigurationManager configManager,
        IEnumerable<string>? allowedDynamicTools = null)
    {
        _serviceProvider = serviceProvider;
        _sessionTools = sessionTools;
        _logger = logger;
        _configManager = configManager;

        if (allowedDynamicTools != null)
            foreach (var tool in allowedDynamicTools)
                _allowedDynamicTools.Add(tool);

        // Track initially loaded tool groups based on assumptions we make in AgentFactory
        _loadedToolGroups.Add("FileSystemTool");
        _loadedToolGroups.Add("BashTool");
    }

    [Description("Gets a list of all available tool groups that can be dynamically loaded into the current session.")]
    public IEnumerable<string> GetAvailableTools()
    {
        var allAvailable = _configManager.GetAllAvailableToolGroups();

        // Only show tools that are in the allowed dynamic list for this agent
        return allAvailable.Where(t => _allowedDynamicTools.Contains(t));
    }

    [Description("Gets the list of tools that are currently loaded and active in the session.")]
    public IEnumerable<string> GetLoadedTools()
    {
        return _loadedToolGroups;
    }

    [Description(
        "Loads a specific tool group (e.g., 'WeatherTool' or 'MCP:MyServer') into the current session so it can be used in subsequent requests.")]
    public async Task<string> LoadToolGroup(
        [Description("The exact name of the tool group to load, as retrieved from GetAvailableTools.")]
        string toolGroupName)
    {
        if (string.IsNullOrWhiteSpace(toolGroupName)) return "Error: Please provide a valid tool group name.";

        if (_loadedToolGroups.Contains(toolGroupName))
            return
                $"Tool group {toolGroupName} is already loaded in the current session. You can use its functions immediately.";

        if (!_allowedDynamicTools.Contains(toolGroupName))
            return
                $"Error: Tool group '{toolGroupName}' is not authorized for dynamic loading in this agent's configuration.";

        IEnumerable<AITool> newTools;

        try
        {
            newTools = toolGroupName switch
            {
                "WeatherTool" => _serviceProvider.GetRequiredService<WeatherTool.WeatherTool>().AsAITools(),
                "CliTool" => _serviceProvider.GetRequiredService<CliTool.CliTool>().AsAITools(),
                "BashTool" => _serviceProvider.GetRequiredService<BashTool.BashTool>().AsAITools(),
                "TodoTool" => _serviceProvider.GetRequiredService<TodoTool.TodoTool>().AsAITools(),
                "WebTool" => _serviceProvider.GetRequiredService<WebTool.WebTool>().AsAITools(),
                "CodeSearchTool" => _serviceProvider.GetRequiredService<CodeSearchTool.CodeSearchTool>().AsAITools(),
                "FileSystemTool" => _serviceProvider.GetRequiredService<FileSystemTool.FileSystemTool>().AsAITools(),
                "WorkflowTool" => _serviceProvider.GetRequiredService<WorkflowTool.WorkflowTool>().AsAITools(),
                "WorkflowStateTool" => _serviceProvider.GetRequiredService<WorkflowStateTool.WorkflowStateTool>()
                    .AsAITools(),
                "RestApiTool" => _serviceProvider.GetRequiredService<RestApiTool.RestApiTool>().AsAITools(),
                "HtmlToMarkdownTool" => _serviceProvider.GetRequiredService<HtmlToMarkdownTool.HtmlToMarkdownTool>()
                    .AsAITools(),
                _ when toolGroupName.StartsWith("MCP:", StringComparison.OrdinalIgnoreCase) => await LoadMcpToolsAsync(
                    toolGroupName[4..]),
                _ => Array.Empty<AITool>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load tool group {ToolGroupName}", toolGroupName);
            return $"Error: Failed to load tool group {toolGroupName}. Details: {ex.Message}";
        }


        if (!newTools.Any())
            return
                $"Error: The tool group '{toolGroupName}' was not found or has no tools. Please check GetAvailableTools for valid values.";

        foreach (var tool in newTools) _sessionTools.Add(tool);

        _loadedToolGroups.Add(toolGroupName);
        _logger.LogInformation("Successfully loaded tool group {ToolGroupName} into session", toolGroupName);

        return
            $"Successfully loaded {toolGroupName}. The functions from this tool group are now available for you to call.";
    }

    public async Task<IEnumerable<AITool>> LoadMcpToolsAsync(string serverName)
    {
        var server = _configManager.GetMcpConfiguration().Servers
            .FirstOrDefault(s => s.Name.Equals(serverName, StringComparison.OrdinalIgnoreCase));

        if (server == null)
        {
            _logger.LogWarning("MCP Server {ServerName} not found in configuration", serverName);
            return Array.Empty<AITool>();
        }

        try
        {
            McpClient mcpClient;
            if (server.Type == "stdio")
            {
                var envVars = new Dictionary<string, string>();
                foreach (var req in server.Requirements)
                {
                    var val = _configManager.GetMcpSecret(req.Key);
                    if (!string.IsNullOrEmpty(val)) envVars[req.Key] = val;
                }

                mcpClient = await McpClient.CreateAsync(new StdioClientTransport(new StdioClientTransportOptions
                {
                    Command = server.Command,
                    Arguments = server.Args,
                    EnvironmentVariables = envVars
                }));
            }

            else
            {
                mcpClient = await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
                {
                    TransportMode = HttpTransportMode.StreamableHttp,
                    Endpoint = new Uri(server.Url)
                }));
            }

            var tools = await mcpClient.ListToolsAsync();
            _logger.LogInformation("Successfully loaded {Count} tools from MCP server {ServerName}", tools.Count,
                serverName);
            return tools;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load tools from MCP server {ServerName}", serverName);
            throw; // Will be caught by the caller
        }
    }

    public IEnumerable<AITool> AsAITools()
    {
        yield return AIFunctionFactory.Create(GetAvailableTools);
        yield return AIFunctionFactory.Create(GetLoadedTools);
        yield return AIFunctionFactory.Create(LoadToolGroup);
    }
}