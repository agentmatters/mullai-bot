using System.Text;
using System.Threading.Channels;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using Mullai.Abstractions.Agents;
using Mullai.Abstractions.Execution;
using Mullai.Abstractions.Messaging;
using Mullai.Abstractions.Orchestration;
using Mullai.Agents;

using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Mullai.Execution.Actors;

public class AgentActor : IAgentActor
{
    private readonly string _agentName;
    private readonly AgentFactory _agentFactory;
    private readonly IConversationManager _conversationManager;
    private readonly IEventBus _eventBus;
    private readonly IWorkflowEngine _workflow;
    private readonly Random _random = new();
    private readonly Channel<TaskExecutionRequest> _mailbox;
    private readonly CancellationTokenSource _cts = new();
    private Task? _processingTask;

    public string AgentName => _agentName;

    public AgentActor(
        string agentName,
        AgentFactory agentFactory,
        IConversationManager conversationManager,
        IEventBus eventBus,
        IWorkflowEngine workflow)
    {
        _agentName = agentName;
        _agentFactory = agentFactory;
        _conversationManager = conversationManager;
        _eventBus = eventBus;
        _workflow = workflow;
        _mailbox = Channel.CreateUnbounded<TaskExecutionRequest>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        Start();
    }

    private void Start()
    {
        _processingTask = Task.Run(() => ProcessMailboxAsync(_cts.Token));
    }

    public async Task SendAsync(TaskExecutionRequest request, CancellationToken cancellationToken = default)
    {
        await _mailbox.Writer.WriteAsync(request, cancellationToken);
    }

    private async Task ProcessMailboxAsync(CancellationToken cancellationToken)
    {
        await foreach (var request in _mailbox.Reader.ReadAllAsync(cancellationToken))
        {
            await ExecuteWithRetryAsync(request, cancellationToken);
        }
    }

    private async Task ExecuteWithRetryAsync(TaskExecutionRequest request, CancellationToken ct)
    {
        int retries = 0;
        const int maxRetries = 2;
        while (retries <= maxRetries)
        {
            try { await ExecuteInternalAsync(request, retries, ct); return; }
            catch (Exception ex)
            {
                retries++;
                if (retries > maxRetries) { await _eventBus.PublishAsync(new TaskStatusEvent(request.Node.Id, request.Node.TraceId ?? "", "Failed", ex.Message), ct); return; }
                await _eventBus.PublishAsync(new TaskStatusEvent(request.Node.Id, request.Node.TraceId ?? "", "InProgress", $"Crashed. Retrying {retries}/{maxRetries}"), ct);
                await Task.Delay(1000, ct); 
            }
        }
    }

    private async Task ExecuteInternalAsync(TaskExecutionRequest request, int attempt, CancellationToken cancellationToken)
    {
        var node = request.Node;
        var sessionId = request.SessionId;
        var traceId = node.TraceId ?? "";

        await _eventBus.PublishAsync(new TaskStatusEvent(node.Id, traceId, "InProgress", $"[{_agentName}] Active"), cancellationToken);
        
        // Simulating transient failures
        if (attempt == 0 && _random.Next(1, 101) <= 20) throw new Exception("Transient dependency failure");

        // DYNAMIC SUB-TASKING (Architect)
        if (_agentName == "Architect" && node.Description.Contains("complex"))
        {
            var subTask = new TaskNode 
            { 
                Id = node.Id + "-sub", 
                Description = "SQL Schema Optimization", 
                AssignedAgent = "DatabaseExpert", 
                TraceId = traceId, 
                Priority = 5 
            };
            // Propagate ExecutionMode and other metadata
            foreach (var kvp in node.Metadata) subTask.Metadata[kvp.Key] = kvp.Value;
            
            await _workflow.SubmitGraphAsync(new[] { subTask }, sessionId);
        }

        var agent = node.AgentDefinition != null
            ? _agentFactory.CreateAgent(new AgentDefinition 
              { 
                  Name = node.AgentDefinition.Name, 
                  Instructions = node.AgentDefinition.Instructions, 
                  Tools = node.AgentDefinition.Tools, 
                  MemoryContexts = node.AgentDefinition.MemoryContexts,
                  Metadata = node.AgentDefinition.Metadata 
              })
            : _agentFactory.CreateAgent(new AgentDefinition { Name = _agentName, Metadata = node.Metadata });
        
        // Get or Create session
        var session = await _conversationManager.GetSessionAsync(sessionId, cancellationToken)
                     ?? await _conversationManager.CreateSessionAsync(sessionId, cancellationToken);
        
        // Load history for context-aware execution
        var history = new List<ChatMessage>();
        await foreach (var msg in _conversationManager.GetHistoryAsync(sessionId, cancellationToken))
        {
            history.Add(msg);
        }

        double costFactor = _agentName switch { "Architect" => 0.05, "DatabaseExpert" => 0.08, "Coder" => 0.03, "Tester" => 0.01, _ => 0.02 };

        var fullResponse = new StringBuilder();
        await foreach (var update in agent.RunStreamingAsync(node.Description, session, cancellationToken))
        {
            var token = update?.ToString() ?? "";
            if (!string.IsNullOrEmpty(token))
            {
                fullResponse.Append(token);
                await _eventBus.PublishAsync(new TokenReceivedEvent(node.Id, token, agent.Name), cancellationToken);
            }
            // Economics: Charge per token
            await _eventBus.PublishAsync(new CostUpdateEvent(traceId, costFactor), cancellationToken);
        }
        
        // Persist the agent's response for future turns
        if (fullResponse.Length > 0)
        {
            await _conversationManager.AddMessageAsync(sessionId, new ChatMessage(ChatRole.Assistant, fullResponse.ToString()), cancellationToken);
        }

        // AGENT AUTONOMY (Tester)
        if (_agentName == "Tester" && _random.Next(1, 4) == 1)
        {
            await _workflow.SubmitGraphAsync(new[] { new TaskNode { Id = Guid.NewGuid().ToString()[..4], Description = "Fix bug discovered by Tester", AssignedAgent = "Coder", TraceId = traceId, Priority = 8 } }, sessionId);
        }

        await _eventBus.PublishAsync(new TraceUpdateEvent(traceId, node.Id, _agentName, $"Completed: {node.Description}"), cancellationToken);
        await _eventBus.PublishAsync(new TaskStatusEvent(node.Id, traceId, "Completed"), cancellationToken);
    }

    public async Task StopAsync()
    {
        _cts.Cancel();
        _mailbox.Writer.Complete();
        if (_processingTask != null)
        {
            await _processingTask;
        }
    }
}
