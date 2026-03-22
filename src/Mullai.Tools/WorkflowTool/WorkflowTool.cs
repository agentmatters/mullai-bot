using System.ComponentModel;
using Microsoft.Extensions.AI;
using Mullai.Workflows.Abstractions;
using Mullai.Workflows.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Mullai.Tools.WorkflowTool;

/// <summary>
/// Tool for managing workflow YAML files stored under ~/.mullai/workflows.
/// </summary>
[Description("A tool for reading, creating, and updating workflow YAML definitions.")]
public sealed class WorkflowTool
{
    private readonly IWorkflowRegistry _registry;
    private readonly IWorkflowRegistryReloader _reloader;
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;

    public WorkflowTool(IWorkflowRegistry registry, IWorkflowRegistryReloader reloader)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _reloader = reloader ?? throw new ArgumentNullException(nameof(reloader));
        _serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    [Description("Lists all workflows currently loaded from ~/.mullai/workflows.")]
    public Task<IReadOnlyList<WorkflowDefinition>> ListWorkflows()
    {
        return Task.FromResult<IReadOnlyList<WorkflowDefinition>>(_registry.GetAll());
    }

    [Description("Reads the YAML definition for a workflow by id.")]
    public Task<string> ReadWorkflowYaml(
        [Description("The workflow id (e.g. bilingual-reply).")] string workflowId)
    {
        var filePath = FindWorkflowFile(workflowId);
        if (filePath is null)
        {
            return Task.FromResult($"Workflow file for '{workflowId}' was not found.");
        }

        return Task.FromResult(File.ReadAllText(filePath));
    }

    [Description("Creates a new workflow from a YAML document.")]
    public Task<string> CreateWorkflowFromYaml(
        [Description("The YAML document to write.")] string yaml,
        [Description("Optional filename (e.g. my-workflow.yml). If omitted, uses <id>.yml.")] string? fileName = null)
    {
        var definition = _deserializer.Deserialize<WorkflowDefinition>(yaml);
        if (definition is null || string.IsNullOrWhiteSpace(definition.Id))
        {
            return Task.FromResult("YAML must include an 'id' field.");
        }

        var path = ResolveWorkflowFile(definition.Id, fileName);
        if (File.Exists(path))
        {
            return Task.FromResult($"Workflow file already exists: {path}");
        }

        File.WriteAllText(path, yaml);
        _reloader.Reload();
        return Task.FromResult($"Created workflow at {path}");
    }

    [Description("Updates an existing workflow by id with a YAML document.")]
    public Task<string> UpdateWorkflowFromYaml(
        [Description("The workflow id to update.")] string workflowId,
        [Description("The YAML document to write.")] string yaml)
    {
        var path = ResolveWorkflowFile(workflowId, null);
        if (!File.Exists(path))
        {
            return Task.FromResult($"Workflow file not found: {path}");
        }

        File.WriteAllText(path, yaml);
        _reloader.Reload();
        return Task.FromResult($"Updated workflow at {path}");
    }

    [Description("Creates or updates a workflow using a structured definition. Read the sample YAML first to understand all supported fields.")]
    public Task<string> UpsertWorkflow(
        [Description("Workflow definition object.")] WorkflowDefinition definition)
    {
        if (definition is null || string.IsNullOrWhiteSpace(definition.Id))
        {
            return Task.FromResult("Workflow definition must include an id.");
        }

        var yaml = _serializer.Serialize(definition);
        var path = ResolveWorkflowFile(definition.Id, null);
        File.WriteAllText(path, yaml);
        _reloader.Reload();
        return Task.FromResult($"Saved workflow at {path}");
    }

    public IEnumerable<AITool> AsAITools()
    {
        yield return AIFunctionFactory.Create(this.ListWorkflows);
        yield return AIFunctionFactory.Create(this.ReadWorkflowYaml);
        yield return AIFunctionFactory.Create(this.CreateWorkflowFromYaml);
        yield return AIFunctionFactory.Create(this.UpdateWorkflowFromYaml);
        yield return AIFunctionFactory.Create(this.UpsertWorkflow);
        yield return AIFunctionFactory.Create(this.GetSampleWorkflowYaml);
        yield return AIFunctionFactory.Create(this.GetIncrementalSummaryWorkflowYaml);
    }

    [Description("Returns a sample workflow YAML that covers all currently supported features (agents, triggers, outputs).")]
    public Task<string> GetSampleWorkflowYaml()
    {
        return Task.FromResult(SampleYaml);
    }

    private const string SampleYaml =
        "id: demo-workflow\n" +
        "name: Demo Workflow\n" +
        "description: Demonstrates agents, triggers, and outputs.\n" +
        "enabled: true\n" +
        "kind: ParallelAgents\n" +
        "agents:\n" +
        "  - name: EnglishAgent\n" +
        "    displayName: English\n" +
        "    instructions: \"You are a helpful assistant who always responds in English.\"\n" +
        "  - name: FrenchAgent\n" +
        "    displayName: French\n" +
        "    instructions: \"You are a helpful assistant who always responds in French.\"\n" +
        "triggers:\n" +
        "  - type: cron\n" +
        "    name: every-10s\n" +
        "    cron: \"*/10 * * * * *\"\n" +
        "    input: \"Give me a bilingual status update.\"\n" +
        "  - type: interval\n" +
        "    name: every-30s\n" +
        "    intervalSeconds: 30\n" +
        "    input: \"Say hello in two languages.\"\n" +
        "  - type: webhook\n" +
        "    id: external-hook\n" +
        "    name: external-hook\n" +
        "    input: \"Incoming payload: {{payload}}\"\n" +
        "    properties:\n" +
        "      secret: \"super-secret\"\n" +
        "outputs:\n" +
        "  - type: log\n" +
        "  - type: webhook\n" +
        "    target: \"https://example.com/mullai-webhook\"\n" +
        "    properties:\n" +
        "      retries: \"3\"\n" +
        "      retryDelaySeconds: \"5\"\n" +
        "      header:X-Auth: \"token123\"\n" +
        "  - type: workflow\n" +
        "    target: concise-assistant\n" +
        "    properties:\n" +
        "      input: \"Summarize this response in one paragraph: {{response}}\"\n";

    [Description("Returns a workflow YAML template for incrementally summarizing a large codebase using workflow state.")]
    public Task<string> GetIncrementalSummaryWorkflowYaml()
    {
        return Task.FromResult(IncrementalSummaryYaml);
    }

    private const string IncrementalSummaryYaml =
        "id: project-summary\n" +
        "name: Project Summary (Incremental)\n" +
        "description: Summarizes one file per run and stores progress in workflow state.\n" +
        "enabled: true\n" +
        "kind: SingleAgent\n" +
        "agents:\n" +
        "  - name: SummarizerAgent\n" +
        "    displayName: Summarizer\n" +
        "    instructions: |\n" +
        "      You are summarizing a large codebase incrementally.\n" +
        "      Use WorkflowStateTool to read state key 'summary.cursor'.\n" +
        "      If missing, initialize it with {\"files\":[],\"index\":0,\"root\":\"/path\"}.\n" +
        "      Use FileSystemTool to list files and read the next file.\n" +
        "      Write a short summary as a comment at the top of the file.\n" +
        "      Increment index and save back to 'summary.cursor'.\n" +
        "      When index >= files.length, set state key 'summary.done' to true.\n" +
        "triggers:\n" +
        "  - type: interval\n" +
        "    name: every-30s\n" +
        "    intervalSeconds: 30\n" +
        "    input: \"Summarize the next file.\"\n" +
        "    properties:\n" +
        "      stopKey: \"summary.done\"\n" +
        "      stopValue: \"true\"\n" +
        "outputs:\n" +
        "  - type: log\n";

    private static string ResolveWorkflowDirectory()
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var configDir = Path.Combine(homeDir, ".mullai");
        var workflowDir = Path.Combine(configDir, "workflows");
        Directory.CreateDirectory(workflowDir);
        return workflowDir;
    }

    private static string ResolveWorkflowFile(string workflowId, string? fileName)
    {
        var workflowDir = ResolveWorkflowDirectory();
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            return Path.Combine(workflowDir, fileName);
        }

        return Path.Combine(workflowDir, $"{workflowId}.yml");
    }

    private static string? FindWorkflowFile(string workflowId)
    {
        var workflowDir = ResolveWorkflowDirectory();
        var candidates = Directory.EnumerateFiles(workflowDir, "*.*", SearchOption.TopDirectoryOnly)
            .Where(path => path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) ||
                           path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase));

        foreach (var file in candidates)
        {
            try
            {
                var yaml = File.ReadAllText(file);
                if (yaml.Contains($"id: {workflowId}", StringComparison.OrdinalIgnoreCase))
                {
                    return file;
                }
            }
            catch
            {
                // Ignore unreadable files.
            }
        }

        return null;
    }
}
