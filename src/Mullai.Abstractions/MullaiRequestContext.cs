namespace Mullai.Abstractions;

public static class MullaiRequestContext
{
    private static readonly AsyncLocal<MullaiRequestInfo?> _current = new();

    public static MullaiRequestInfo? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}

public sealed record MullaiRequestInfo
{
    public string? Provider { get; init; }
    public string? Model { get; init; }
}