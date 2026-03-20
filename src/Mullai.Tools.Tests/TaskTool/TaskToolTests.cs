using Moq;
using Mullai.Abstractions.Orchestration;
using Mullai.Tools.TaskTool;
using Xunit;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Mullai.Tools.Tests.TaskTool;

public class TaskToolTests
{
    private readonly Mock<IWorkflowEngine> _workflowMock;
    private readonly Mock<IConversationManager> _conversationMock;
    private readonly Mullai.Tools.TaskTool.TaskTool _taskTool;

    public TaskToolTests()
    {
        _workflowMock = new Mock<IWorkflowEngine>();
        _conversationMock = new Mock<IConversationManager>();
        _taskTool = new Mullai.Tools.TaskTool.TaskTool(_workflowMock.Object, _conversationMock.Object);
    }

    [Fact]
    public async Task CreateTask_ShouldSubmitTaskToWorkflowEngine()
    {
        // Arrange
        var description = "Test task";
        var agent = "Assistant";

        // Act
        var result = await _taskTool.CreateTask(description, agent);

        // Assert
        _workflowMock.Verify(w => w.SubmitGraphAsync(
            It.Is<IEnumerable<TaskNode>>(nodes => 
                nodes.Count() == 1 && 
                nodes.First().Description == description && 
                nodes.First().AssignedAgent == agent &&
                nodes.First().SessionId != string.Empty),
            It.IsAny<string>()), Times.Once);

        Assert.Contains("Task created successfully", result);
    }

    [Fact]
    public async Task GetTaskStatus_ShouldReturnStatusFromWorkflowEngine()
    {
        // Arrange
        var taskId = "test-id";
        var task = new TaskNode { Id = taskId, Status = Mullai.Abstractions.Orchestration.TaskStatus.Running, AssignedAgent = "Assistant", Description = "Test" };
        _workflowMock.Setup(w => w.GetTaskAsync(taskId)).ReturnsAsync(task);

        // Act
        var result = await _taskTool.GetTaskStatus(taskId);

        // Assert
        Assert.Contains("Status: Running", result);
        Assert.Contains("TaskId: test-id", result);
    }

    [Fact]
    public async Task ListTasks_ShouldReturnTasksFromWorkflowEngine()
    {
        // Arrange
        var tasks = new List<TaskNode>
        {
            new TaskNode { Id = "1", Status = Mullai.Abstractions.Orchestration.TaskStatus.Pending, AssignedAgent = "Assistant", Description = "Task 1" },
            new TaskNode { Id = "2", Status = Mullai.Abstractions.Orchestration.TaskStatus.Running, AssignedAgent = "Coder", Description = "Task 2" }
        };
        _workflowMock.Setup(w => w.GetTasksAsync(It.IsAny<string>())).ReturnsAsync(tasks);

        // Act
        var result = await _taskTool.ListTasks();

        // Assert
        Assert.Contains("[1] Pending", result);
        Assert.Contains("[2] Running", result);
        Assert.Contains("Assistant: Task 1", result);
        Assert.Contains("Coder: Task 2", result);
    }

    [Fact]
    public async Task WaitTask_ShouldCallWaitForTaskAsyncAndReturnOutput()
    {
        // Arrange
        var taskId = "test-id";
        _workflowMock.Setup(w => w.WaitForTaskAsync(taskId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.Assistant, "Task output here") 
            { 
                AdditionalProperties = new AdditionalPropertiesDictionary { ["TaskId"] = taskId, ["AgentName"] = "Assistant" } 
            }
        };
        _conversationMock.Setup(c => c.GetHistoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(messages.ToAsyncEnumerable());

        // Act
        var result = await _taskTool.WaitTask(taskId);

        // Assert
        _workflowMock.Verify(w => w.WaitForTaskAsync(taskId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains("Task output here", result);
    }

    [Fact]
    public async Task SendMessageToAgent_ShouldSubmitFollowUpTask()
    {
        // Arrange
        var agent = "Coder";
        var message = "Fix the bug";

        // Act
        var result = await _taskTool.SendMessageToAgent(agent, message);

        // Assert
        _workflowMock.Verify(w => w.SubmitGraphAsync(
            It.Is<IEnumerable<TaskNode>>(nodes => 
                nodes.Count() == 1 && 
                nodes.First().Description.Contains(message) && 
                nodes.First().AssignedAgent == agent),
            It.IsAny<string>()), Times.Once);

        Assert.Contains("Message sent", result);
    }

    [Fact]
    public async Task ReadTaskOutput_ShouldReturnMessageWithMatchingTaskId()
    {
        // Arrange
        var taskId = "match-123";
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.User, "Hi"),
            new ChatMessage(ChatRole.Assistant, "Result of work") 
            { 
                AdditionalProperties = new AdditionalPropertiesDictionary { ["TaskId"] = taskId, ["AgentName"] = "Coder" } 
            }
        };
        _conversationMock.Setup(c => c.GetHistoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(messages.ToAsyncEnumerable());

        // Act
        var result = await _taskTool.ReadTaskOutput(taskId);

        // Assert
        Assert.Contains("Result of work", result);
        Assert.Contains("Coder", result);
    }

    [Fact]
    public async Task AskAgent_ShouldCreateWaitAndRead()
    {
        // Arrange
        var agent = "DatabaseExpert";
        var question = "What is the schema?";
        
        // IMPORTANT: TaskTool.AskAgent generates a random TaskId. 
        // We need to capture it or ensure the mock history returns something for *any* taskId that matches the agent.
        // For testing we will ensure the history contains the specific text we expect.
        
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.Assistant, "Schema is SQL") 
            { 
                AdditionalProperties = new AdditionalPropertiesDictionary { ["TaskId"] = "any-random-id", ["AgentName"] = agent } 
            }
        };
        
        // Since AskAgent calls WaitTask -> ReadTaskOutput, we need history to match 
        // We'll mock history to return a message where TaskId matches whatever CreateTask returns.
        // But CreateTask is internal. Let's just mock GetHistory to always return our "Schema is SQL" message
        // and override the TaskId check in ReadTaskOutput for this test if possible? No.
        
        // Better: Mock SubmitGraphAsync to set a known TaskId on the node.
        _workflowMock.Setup(w => w.SubmitGraphAsync(It.IsAny<IEnumerable<TaskNode>>(), It.IsAny<string>()))
            .Callback<IEnumerable<TaskNode>, string>((nodes, sid) => nodes.First().Id = "fixed-id")
            .Returns(Task.CompletedTask);

        var messages2 = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.Assistant, "Schema is SQL") 
            { 
                AdditionalProperties = new AdditionalPropertiesDictionary { ["TaskId"] = "fixed-id", ["AgentName"] = agent } 
            }
        };
        _conversationMock.Setup(c => c.GetHistoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(messages2.ToAsyncEnumerable());

        // Act
        var result = await _taskTool.AskAgent(agent, question);

        // Assert
        Assert.Contains("Schema is SQL", result);
    }
}

public static class AsyncEnumerableExtensions
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            yield return item;
            await Task.Yield();
        }
    }
}
