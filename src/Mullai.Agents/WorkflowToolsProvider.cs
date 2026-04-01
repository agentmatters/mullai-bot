using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Mullai.Tools.BashTool;
using Mullai.Tools.CliTool;
using Mullai.Tools.CodeSearchTool;
using Mullai.Tools.FileSystemTool;
using Mullai.Tools.TodoTool;
using Mullai.Tools.WeatherTool;
using Mullai.Tools.WebTool;
using Mullai.Tools.WorkflowStateTool;
using Mullai.Tools.WorkflowTool;
using Mullai.Workflows.Abstractions;

namespace Mullai.Agents;

public sealed class WorkflowToolsProvider : IWorkflowToolsProvider
{
    private readonly IServiceProvider _serviceProvider;

    public WorkflowToolsProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public IReadOnlyList<AITool> GetTools()
    {
        return
        [
            .. _serviceProvider.GetRequiredService<WeatherTool>().AsAITools(),
            .. _serviceProvider.GetRequiredService<CliTool>().AsAITools(),
            .. _serviceProvider.GetRequiredService<BashTool>().AsAITools(),
            .. _serviceProvider.GetRequiredService<TodoTool>().AsAITools(),
            .. _serviceProvider.GetRequiredService<WebTool>().AsAITools(),
            .. _serviceProvider.GetRequiredService<CodeSearchTool>().AsAITools(),
            .. _serviceProvider.GetRequiredService<FileSystemTool>().AsAITools(),
            .. _serviceProvider.GetRequiredService<WorkflowTool>().AsAITools(),
            .. _serviceProvider.GetRequiredService<WorkflowStateTool>().AsAITools()
        ];
    }
}