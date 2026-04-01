using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Mullai.Abstractions.WorkflowState;

namespace Mullai.Tools.WorkflowStateTool;

/// <summary>
///     Tool for storing workflow state in SQLite.
/// </summary>
[Description("A tool for reading and updating workflow state for long-running workflows.")]
public sealed class WorkflowStateTool(IWorkflowStateStore stateStore)
{
    [Description("Gets a workflow state value by key.")]
    public async Task<WorkflowStateRecord?> GetState(
        [Description("Workflow id.")] string workflowId,
        [Description("State key.")] string key)
    {
        return await stateStore.GetAsync(workflowId, key);
    }

    [Description("Lists all state entries for a workflow.")]
    public async Task<IReadOnlyCollection<WorkflowStateRecord>> ListState(
        [Description("Workflow id.")] string workflowId)
    {
        return await stateStore.GetAllAsync(workflowId);
    }

    [Description("Upserts a workflow state value (JSON string).")]
    public async Task<string> SetState(
        [Description("Workflow id.")] string workflowId,
        [Description("State key.")] string key,
        [Description("JSON string value.")] string jsonValue)
    {
        EnsureJson(jsonValue);
        await stateStore.UpsertAsync(workflowId, key, jsonValue);
        return "OK";
    }

    [Description("Deletes a workflow state key.")]
    public async Task<string> DeleteState(
        [Description("Workflow id.")] string workflowId,
        [Description("State key.")] string key)
    {
        await stateStore.RemoveAsync(workflowId, key);
        return "OK";
    }

    public IEnumerable<AITool> AsAITools()
    {
        yield return AIFunctionFactory.Create(GetState);
        yield return AIFunctionFactory.Create(ListState);
        yield return AIFunctionFactory.Create(SetState);
        yield return AIFunctionFactory.Create(DeleteState);
    }

    private static void EnsureJson(string json)
    {
        try
        {
            JsonDocument.Parse(json);
        }
        catch (Exception ex)
        {
            throw new ArgumentException("jsonValue must be valid JSON.", nameof(json), ex);
        }
    }
}