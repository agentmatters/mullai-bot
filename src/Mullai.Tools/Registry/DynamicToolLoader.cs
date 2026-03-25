using System.ComponentModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mullai.Tools.WeatherTool;
using Mullai.Tools.CliTool;
using Mullai.Tools.BashTool;
using Mullai.Tools.TodoTool;
using Mullai.Tools.WebTool;
using Mullai.Tools.CodeSearchTool;
using Mullai.Tools.FileSystemTool;
using Mullai.Tools.WorkflowTool;
using Mullai.Tools.WorkflowStateTool;
using Mullai.Tools.RestApiTool;
using Mullai.Tools.HtmlToMarkdownTool;

namespace Mullai.Tools.Registry;

[Description("A tool to discover and dynamically load other available tools into the current agent session.")]
public class DynamicToolLoader
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IList<AITool> _sessionTools;
    private readonly ILogger<DynamicToolLoader> _logger;
    private readonly HashSet<string> _loadedToolGroups = new(StringComparer.OrdinalIgnoreCase);

    public DynamicToolLoader(IServiceProvider serviceProvider, IList<AITool> sessionTools, ILogger<DynamicToolLoader> logger)
    {
        _serviceProvider = serviceProvider;
        _sessionTools = sessionTools;
        _logger = logger;
        
        // Track initially loaded tool groups based on assumptions we make in AgentFactory
        _loadedToolGroups.Add("FileSystemTool");
        _loadedToolGroups.Add("BashTool");
    }

    [Description("Gets a list of all available tool groups that can be dynamically loaded into the current session.")]
    public IEnumerable<string> GetAvailableTools()
    {
        var allTools = new[]
        {
            "WeatherTool",
            "CliTool",
            "BashTool",
            "TodoTool",
            "WebTool",
            "CodeSearchTool",
            "FileSystemTool",
            "WorkflowTool",
            "WorkflowStateTool",
            "RestApiTool",
            "HtmlToMarkdownTool"
        };
        
        return allTools;
    }

    [Description("Gets the list of tools that are currently loaded and active in the session.")]
    public IEnumerable<string> GetLoadedTools()
    {
        return _loadedToolGroups;
    }

    [Description("Loads a specific tool group (e.g., 'WeatherTool') into the current session so it can be used in subsequent requests.")]
    public string LoadToolGroup([Description("The exact name of the tool group to load, as retrieved from GetAvailableTools.")] string toolGroupName)
    {
        if (string.IsNullOrWhiteSpace(toolGroupName))
        {
            return "Error: Please provide a valid tool group name.";
        }

        if (_loadedToolGroups.Contains(toolGroupName))
        {
            return $"Tool group {toolGroupName} is already loaded in the current session. You can use its functions immediately.";
        }

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
                "WorkflowStateTool" => _serviceProvider.GetRequiredService<WorkflowStateTool.WorkflowStateTool>().AsAITools(),
                "RestApiTool" => _serviceProvider.GetRequiredService<RestApiTool.RestApiTool>().AsAITools(),
                "HtmlToMarkdownTool" => _serviceProvider.GetRequiredService<HtmlToMarkdownTool.HtmlToMarkdownTool>().AsAITools(),
                _ => Array.Empty<AITool>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load tool group {ToolGroupName}", toolGroupName);
            return $"Error: Required services for {toolGroupName} could not be resolved. Please ensure they are registered.";
        }

        if (!newTools.Any())
        {
            return $"Error: The tool group '{toolGroupName}' was not found or has no tools. Please check GetAvailableTools for valid values.";
        }

        foreach (var tool in newTools)
        {
            _sessionTools.Add(tool);
        }

        _loadedToolGroups.Add(toolGroupName);
        _logger.LogInformation("Successfully loaded tool group {ToolGroupName} into session", toolGroupName);

        return $"Successfully loaded {toolGroupName}. The functions from this tool group are now available for you to call.";
    }

    public IEnumerable<AITool> AsAITools()
    {
        yield return AIFunctionFactory.Create(GetAvailableTools);
        yield return AIFunctionFactory.Create(GetLoadedTools);
        yield return AIFunctionFactory.Create(LoadToolGroup);
    }
}
