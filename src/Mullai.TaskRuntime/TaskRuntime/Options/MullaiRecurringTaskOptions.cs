namespace Mullai.TaskRuntime.Options;

public sealed class MullaiRecurringTaskOptions
{
    public const string SectionName = "Mullai:RecurringTasks";

    public List<MullaiRecurringTaskDefinition> Jobs { get; set; } = [];
}

public sealed class MullaiRecurringTaskDefinition
{
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool RunOnStartup { get; set; }
    public int IntervalSeconds { get; set; } = 300;
    public string SessionKey { get; set; } = string.Empty;
    public string AgentName { get; set; } = "Assistant";
    public string Prompt { get; set; } = string.Empty;
    public int? MaxAttempts { get; set; }
}
