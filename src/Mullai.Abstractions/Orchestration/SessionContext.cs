using System.Threading;

namespace Mullai.Abstractions.Orchestration;

public static class SessionContext
{
    private static readonly AsyncLocal<string?> _currentSessionId = new();

    public static string? CurrentSessionId
    {
        get => _currentSessionId.Value;
        set => _currentSessionId.Value = value;
    }
}
