namespace Mullai.Web.Models;

public class ComposerSubmitArgs
{
    public string Prompt { get; init; } = string.Empty;
    public string? Provider { get; init; }
    public string? Model { get; init; }
    public string? Agent { get; init; }
}
