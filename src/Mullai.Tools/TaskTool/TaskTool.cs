using System.ComponentModel;
using Microsoft.Extensions.AI;
using Mullai.Abstractions.Orchestration;

namespace Mullai.Tools.TaskTool;

/// <summary>
/// Tool for managing tasks and sub-tasks within the Mullai agent fabric.
/// </summary>
public class TaskTool(IWorkflowEngine workflowEngine, IConversationManager conversationManager)
{
    /// <summary>
    /// Creates a new task and assigns it to an agent.
    /// </summary>
    /// <param name="description">The detailed description of the task.</param>
    /// <param name="assignedAgent">The name of the agent to perform the task (e.g., Assistant, Coder, Tester).</param>
    /// <param name="priority">The priority of the task (higher is more urgent).</param>
    /// <param name="requiresApproval">Whether the task requires user approval before execution.</param>
    /// <returns>A confirmation message including the new Task ID.</returns>
    [Description("Creates a new task and assigns it to a specialized agent. Use this to delegate work to other agents.")]
    public async Task<string> CreateTask(
        [Description("Detailed description of what the task should accomplish")] string description,
        [Description("The name of the agent to assign (e.g., Assistant, Coder, Tester, DatabaseExpert)")] string assignedAgent,
        [Description("Priority level (0-10)")] int priority = 0,
        [Description("Whether this task needs explicit user approval")] bool requiresApproval = false)
    {
        var task = new TaskNode
        {
            Id = Guid.NewGuid().ToString()[..8],
            Description = description,
            AssignedAgent = assignedAgent,
            Priority = priority,
            RequiresApproval = requiresApproval,
            Status = Mullai.Abstractions.Orchestration.TaskStatus.Pending,
            SessionId = GetSessionId()
        };

        Console.WriteLine($"[DEBUG: FLOW] TaskTool: CreateTask called. Task {task.Id} for {assignedAgent}");
        await workflowEngine.SubmitGraphAsync(new[] { task }, GetSessionId());

        return $"Task created successfully. TaskId: {task.Id}, AssignedTo: {assignedAgent}";
    }

    [Description("Checks the current status of a previously created task.")]
    public async Task<string> GetTaskStatus([Description("The unique Task ID to query")] string taskId)
    {
        Console.WriteLine($"[DEBUG: FLOW] TaskTool: GetTaskStatus called for {taskId}");
        var task = await workflowEngine.GetTaskAsync(taskId);
        if (task == null)
        {
            return $"Task {taskId} not found or already completed/removed.";
        }

        return $"TaskId: {task.Id}, Status: {task.Status}, Agent: {task.AssignedAgent}, Description: {task.Description}";
    }

    [Description("Lists all currently active or pending tasks.")]
    public async Task<string> ListTasks()
    {
        Console.WriteLine($"[DEBUG: FLOW] TaskTool: ListTasks called");
        var tasks = await workflowEngine.GetTasksAsync(GetSessionId());
        var taskList = tasks.Select(t => $"- [{t.Id}] {t.Status} | {t.AssignedAgent}: {t.Description}");
        
        if (!taskList.Any()) return "No active tasks found.";
        
        return "Current Tasks:\n" + string.Join("\n", taskList);
    }

    [Description("Waits for a specific task to complete and returns its final output.")]
    public async Task<string> WaitTask(string taskId)
    {
        Console.WriteLine($"[DEBUG: FLOW] TaskTool: WaitTask called for {taskId}. Start waiting...");
        await workflowEngine.WaitForTaskAsync(taskId);
        Console.WriteLine($"[DEBUG: FLOW] TaskTool: WaitTask for {taskId} completed. Fetching output...");
        
        return await ReadTaskOutput(taskId);
    }

    [Description("Reads the final output/response of a completed task.")]
    public async Task<string> ReadTaskOutput([Description("The Task ID to read")] string taskId)
    {
        var history = new List<ChatMessage>();
        await foreach (var msg in conversationManager.GetHistoryAsync(GetSessionId()))
        {
            if (msg.Role == ChatRole.Assistant && msg.AdditionalProperties?.GetValueOrDefault("TaskId")?.ToString() == taskId)
            {
                return $"Output from task {taskId} ({msg.AdditionalProperties.GetValueOrDefault("AgentName")}):\n{msg.Text}";
            }
        }

        return $"No output found for task {taskId}. The agent may still be working or failed to persist its response.";
    }

    [Description("The ultimate 'collaborate' tool: assigns a task to another agent, waits for it to finish, and returns the result immediately.")]
    public async Task<string> AskAgent(
        [Description("The name of the agent to collaborate with")] string agentName,
        [Description("The question or task for the agent")] string instruction)
    {
        var createTaskResult = await CreateTask(instruction, agentName);
        // Extract TaskId from response "Task created successfully. TaskId: {task.Id}, AssignedTo: {assignedAgent}"
        var taskId = createTaskResult.Split("TaskId: ")[1].Split(",")[0];
        
        return await WaitTask(taskId);
    }

    [Description("Sends further instructions or a message to a specialized agent. Use this to manage ongoing work.")]
    public async Task<string> SendMessageToAgent(
        [Description("The name of the agent to message")] string agentName,
        [Description("The message or further instructions")] string message)
    {
        Console.WriteLine($"[DEBUG: FLOW] TaskTool: SendMessageToAgent called for {agentName}");
        var task = new TaskNode
        {
            Id = Guid.NewGuid().ToString()[..8],
            Description = $"[FOLLOW-UP] {message}",
            AssignedAgent = agentName,
            Priority = 5,
            Status = Mullai.Abstractions.Orchestration.TaskStatus.Pending,
            TraceId = GetSessionId(),
            SessionId = GetSessionId()
        };

        await workflowEngine.SubmitGraphAsync(new[] { task }, GetSessionId());

        return $"Message sent to {agentName}. A follow-up task {task.Id} has been created.";
    }

    /// <summary>
    /// Returns the functions provided by this plugin.
    /// </summary>
    public IEnumerable<AITool> AsAITools()
    {
        yield return AIFunctionFactory.Create(this.CreateTask);
        yield return AIFunctionFactory.Create(this.GetTaskStatus);
        yield return AIFunctionFactory.Create(this.ListTasks);
        yield return AIFunctionFactory.Create(this.WaitTask);
        yield return AIFunctionFactory.Create(this.ReadTaskOutput);
        yield return AIFunctionFactory.Create(this.AskAgent);
        yield return AIFunctionFactory.Create(this.SendMessageToAgent);
    }

    private string GetSessionId()
    {
        return SessionContext.CurrentSessionId ?? "default";
    }
}
