using Microsoft.Extensions.AI;
using Mullai.Abstractions.Agents;
using Mullai.Abstractions.Orchestration;
using Mullai.Agents;
using System.Text.Json;

namespace Mullai.Orchestration;

public class Planner : IPlanner
{
    private readonly AgentFactory _agentFactory;

    public Planner(AgentFactory agentFactory)
    {
        _agentFactory = agentFactory;
    }

    public async Task<TaskGraph> PlanAsync(string userInput, IEnumerable<ChatMessage>? history = null, ExecutionMode mode = ExecutionMode.Team, CancellationToken cancellationToken = default)
    {
        // ── Fast Paths ──────────────────────────────────────────────────────
        if (mode == ExecutionMode.Chat || mode == ExecutionMode.Agent)
        {
            var soloNode = new TaskNode
            {
                Id = Guid.NewGuid().ToString(),
                Description = userInput, // The agent will handle the history themselves
                AssignedAgent = "Assistant",
                Metadata = new Dictionary<string, object> { { "ExecutionMode", mode.ToString() } }
            };
            return new TaskGraph { Nodes = [soloNode] };
        }

        // ── Orchestration (Team Mode) ───────────────────────────────────────
        var historyContext = "";
        if (history != null && history.Any())
        {
            var recent = history.TakeLast(5);
            historyContext = "\nRecent Conversation Context:\n" + string.Join("\n", recent.Select(m => $"{m.Role}: {m.Text}"));
        }

        var prompt = $@"
You are the Mullai Orchestrator. Your goal is to decompose a complex user request into a sequence of actionable tasks.
{historyContext}
User Input: {userInput}
Please provide a JSON array of tasks, where each task is an object with the following properties:
- Id: A unique string identifier for the task.
- Description: A clear and concise description of the task.
- AssignedAgent: The name of the agent best suited to perform this task (e.g., 'Coder', 'Tester', 'Architect', 'Assistant', 'DatabaseExpert').
- Dependencies: An optional array of task Ids that must be completed before this task can start.
- RequiresApproval: An optional boolean indicating if this task requires human approval before execution.

Example:
```json
[
    {{
        ""Id"": ""1"",
        ""Description"": ""Analyze the user's request and identify key components."",
        ""AssignedAgent"": ""Assistant""
    }},
    {{
        ""Id"": ""2"",
        ""Description"": ""Design a database schema based on the identified components."",
        ""AssignedAgent"": ""Architect"",
        ""Dependencies"": [""1""]
    }},
    {{
        ""Id"": ""3"",
        ""Description"": ""Implement the database schema."",
        ""AssignedAgent"": ""DatabaseExpert"",
        ""Dependencies"": [""2""],
        ""RequiresApproval"": true
    }}
]
```
Ensure the JSON is valid and directly parsable. Do not include any additional text or formatting outside the JSON block.
";

        var fastPlan = GetSmartPlan(userInput, "TR-" + Guid.NewGuid().ToString()[..4]);
        if (fastPlan != null)
        {
            foreach (var node in fastPlan.Nodes) node.Metadata["ExecutionMode"] = "Team";
            return fastPlan;
        }

        // 2. Fallback to LLM Planning
        var agent = _agentFactory.GetAgent("Orchestrator");
        var session = await agent.CreateSessionAsync(cancellationToken);
        
        var response = await agent.RunAsync(userInput, session, cancellationToken);
        var json = response?.ToString() ?? string.Empty;

        // Basic clean up in case of markdown blocks
        json = json.Trim();
        if (json.StartsWith("```json")) json = json[7..^3].Trim();
        else if (json.StartsWith("```")) json = json[3..^3].Trim();

        try
        {
            var graph = JsonSerializer.Deserialize<TaskGraph>(json) ?? new TaskGraph();
            foreach (var node in graph.Nodes) 
            {
                node.Metadata["ExecutionMode"] = "Team";
                if (string.IsNullOrEmpty(node.TraceId)) node.TraceId = "TR-" + Guid.NewGuid().ToString()[..4];
            }
            return graph;
        }
        catch
        {
            // Final Fallback
            var node = new TaskNode { Id = "main", Description = userInput, AssignedAgent = "Assistant", TraceId = "TR-" + Guid.NewGuid().ToString()[..4] };
            node.Metadata["ExecutionMode"] = "Team";
            return new TaskGraph { Nodes = new List<TaskNode> { node } };
        }
    }

    private TaskGraph? GetSmartPlan(string input, string traceId)
    {
        var lowerInput = input.ToLower();
        var nodes = new List<TaskNode>();

        if (lowerInput.Contains("fix") || lowerInput.Contains("bug"))
        {
            nodes.Add(new TaskNode { Id = "1", Description = $"Analyze bug: {input}", AssignedAgent = "Coder", TraceId = traceId });
            nodes.Add(new TaskNode { Id = "2", Description = "Verification Test", AssignedAgent = "Tester", TraceId = traceId });
        }
        else if (lowerInput.Contains("database") || lowerInput.Contains("sql") || lowerInput.Contains("data"))
        {
            nodes.Add(new TaskNode { Id = "1", Description = $"Design Schema: {input}", AssignedAgent = "Architect", TraceId = traceId });
            nodes.Add(new TaskNode { Id = "2", Description = "Optimize SQL", AssignedAgent = "DatabaseExpert", TraceId = traceId });
            nodes.Add(new TaskNode { Id = "3", Description = "Implement Data Layer", AssignedAgent = "Coder", TraceId = traceId, RequiresApproval = true });
        }
        else if (lowerInput.Contains("frontend") || lowerInput.Contains("ui") || lowerInput.Contains("css"))
        {
            nodes.Add(new TaskNode { Id = "1", Description = $"UI Mockup for: {input}", AssignedAgent = "Architect", TraceId = traceId });
            nodes.Add(new TaskNode { Id = "2", Description = "Implement Stylings", AssignedAgent = "Coder", TraceId = traceId });
        }
        else return null;

        return new TaskGraph { Nodes = nodes };
    }
}
