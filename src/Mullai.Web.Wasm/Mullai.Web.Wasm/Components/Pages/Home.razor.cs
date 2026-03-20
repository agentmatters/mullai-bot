using Microsoft.AspNetCore.Components;
using Mullai.Web.Wasm.Services;
using Mullai.Abstractions.Orchestration;

namespace Mullai.Web.Wasm.Components.Pages;

public partial class Home : ComponentBase, IAsyncDisposable
{
    [Inject] private ChatState ChatState { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private ExecutionMode currentMode = ExecutionMode.Team;

    protected override async Task OnInitializedAsync()
    {
        ChatState.OnChange += StateHasChanged;
        await ChatState.InitializeAsync();
    }

    private bool IsSettingsView => IsSettingsPath(NavigationManager.Uri);

    private bool IsSettingsPath(string uri)
    {
        var relative = NavigationManager.ToBaseRelativePath(uri);
        return relative.StartsWith("settings", StringComparison.OrdinalIgnoreCase);
    }

    public async ValueTask DisposeAsync()
    {
        ChatState.OnChange -= StateHasChanged;
        // Navigation handling is now in NavMenu, but we could keep it here if needed.
        // However, the original code had it here too.
    }
}
