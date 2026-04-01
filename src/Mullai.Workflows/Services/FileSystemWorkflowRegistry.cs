using Microsoft.Extensions.Logging;
using Mullai.Workflows.Abstractions;
using Mullai.Workflows.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Mullai.Workflows.Services;

public sealed class FileSystemWorkflowRegistry : IWorkflowRegistry, IWorkflowRegistryReloader
{
    private readonly ILogger<FileSystemWorkflowRegistry> _logger;
    private readonly object _sync = new();
    private Dictionary<string, WorkflowDefinition> _byId;
    private IReadOnlyList<WorkflowDefinition> _definitions;

    public FileSystemWorkflowRegistry(ILogger<FileSystemWorkflowRegistry> logger)
    {
        _logger = logger;
        Reload();
    }

    public IReadOnlyList<WorkflowDefinition> GetAll()
    {
        lock (_sync)
        {
            return _definitions;
        }
    }

    public WorkflowDefinition? GetById(string workflowId)
    {
        if (string.IsNullOrWhiteSpace(workflowId)) return null;

        lock (_sync)
        {
            return _byId.TryGetValue(workflowId.Trim(), out var definition) ? definition : null;
        }
    }

    public void Reload()
    {
        var definitions = LoadDefinitions();
        var byId = definitions.ToDictionary(def => def.Id, StringComparer.OrdinalIgnoreCase);

        lock (_sync)
        {
            _definitions = definitions;
            _byId = byId;
        }

        if (definitions.Count == 0)
            _logger.LogWarning("No workflow definitions loaded from ~/.mullai/workflows.");
        else
            _logger.LogInformation(
                "Loaded {WorkflowCount} workflow definition(s): {WorkflowIds}",
                definitions.Count,
                string.Join(", ", definitions.Select(d => d.Id)));
    }

    private IReadOnlyList<WorkflowDefinition> LoadDefinitions()
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var workflowDir = Path.Combine(homeDir, ".mullai", "workflows");
        if (!Directory.Exists(workflowDir))
        {
            _logger.LogInformation("Workflow directory not found at {WorkflowDir}.", workflowDir);
            return Array.Empty<WorkflowDefinition>();
        }

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var results = new List<WorkflowDefinition>();
        var files = Directory.EnumerateFiles(workflowDir, "*.*", SearchOption.TopDirectoryOnly)
            .Where(path => path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) ||
                           path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
            try
            {
                var yaml = File.ReadAllText(file);
                if (string.IsNullOrWhiteSpace(yaml)) continue;

                if (TryDeserializeList(deserializer, yaml, out var list))
                {
                    results.AddRange(list);
                    continue;
                }

                var definition = deserializer.Deserialize<WorkflowDefinition>(yaml);
                if (definition is not null && !string.IsNullOrWhiteSpace(definition.Id)) results.Add(definition);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load workflow definition from {WorkflowFile}.", file);
            }

        return results
            .Where(def => !string.IsNullOrWhiteSpace(def.Id))
            .ToArray();
    }

    private static bool TryDeserializeList(IDeserializer deserializer, string yaml,
        out List<WorkflowDefinition> definitions)
    {
        definitions = [];
        try
        {
            var list = deserializer.Deserialize<List<WorkflowDefinition>>(yaml);
            if (list is null || list.Count == 0) return false;

            definitions = list;
            return true;
        }
        catch
        {
            return false;
        }
    }
}