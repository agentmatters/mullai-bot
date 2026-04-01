using Mullai.Workflows.Abstractions;

namespace Mullai.TaskRuntime.Services.Background;

public sealed class WorkflowRegistryWatcherService : BackgroundService
{
    private readonly ILogger<WorkflowRegistryWatcherService> _logger;
    private readonly IWorkflowRegistryReloader _reloader;
    private Timer? _debounceTimer;
    private FileSystemWatcher? _watcher;

    public WorkflowRegistryWatcherService(
        IWorkflowRegistryReloader reloader,
        ILogger<WorkflowRegistryWatcherService> logger)
    {
        _reloader = reloader ?? throw new ArgumentNullException(nameof(reloader));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var workflowDir = Path.Combine(homeDir, ".mullai", "workflows");
        Directory.CreateDirectory(workflowDir);

        _watcher = new FileSystemWatcher(workflowDir)
        {
            IncludeSubdirectories = false,
            Filter = "*.*",
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime |
                           NotifyFilters.Size
        };

        _watcher.Changed += OnChanged;
        _watcher.Created += OnChanged;
        _watcher.Deleted += OnChanged;
        _watcher.Renamed += OnChanged;
        _watcher.EnableRaisingEvents = true;

        _logger.LogInformation("Watching workflow directory for changes: {WorkflowDir}", workflowDir);

        stoppingToken.Register(() =>
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _debounceTimer?.Dispose();
        });

        return Task.CompletedTask;
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        var extension = Path.GetExtension(e.FullPath);
        if (!extension.Equals(".yml", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".yaml", StringComparison.OrdinalIgnoreCase))
            return;

        _debounceTimer?.Dispose();
        _debounceTimer = new Timer(_ =>
        {
            try
            {
                _reloader.Reload();
                _logger.LogInformation("Workflow registry reloaded after file change.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to reload workflow registry.");
            }
        }, null, TimeSpan.FromMilliseconds(300), Timeout.InfiniteTimeSpan);
    }
}