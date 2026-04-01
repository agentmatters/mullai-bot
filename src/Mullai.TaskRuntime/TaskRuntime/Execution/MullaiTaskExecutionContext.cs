namespace Mullai.TaskRuntime.Execution;

public static class MullaiTaskExecutionContext
{
    private static readonly AsyncLocal<MullaiTaskExecutionScopeData?> CurrentContext = new();

    public static MullaiTaskExecutionScopeData? Current => CurrentContext.Value;

    public static IDisposable BeginScope(string taskId, string sessionKey)
    {
        var previous = CurrentContext.Value;
        CurrentContext.Value = new MullaiTaskExecutionScopeData(taskId, sessionKey);
        return new Scope(previous);
    }

    public sealed record MullaiTaskExecutionScopeData(string TaskId, string SessionKey);

    private sealed class Scope : IDisposable
    {
        private readonly MullaiTaskExecutionScopeData? _previous;
        private bool _isDisposed;

        public Scope(MullaiTaskExecutionScopeData? previous)
        {
            _previous = previous;
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            CurrentContext.Value = _previous;
            _isDisposed = true;
        }
    }
}