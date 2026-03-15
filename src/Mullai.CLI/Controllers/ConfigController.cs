using Spectre.Console;
using Mullai.Abstractions.Configuration;
using Mullai.Providers.Models;
using System.Text.Json;

namespace Mullai.CLI.Controllers;

public class ConfigController
{
    private readonly ICredentialStorage _credentialStorage;
    private readonly string _modelsJsonPath;

    public ConfigController(ICredentialStorage credentialStorage)
    {
        _credentialStorage = credentialStorage;
        _modelsJsonPath = Path.Combine(AppContext.BaseDirectory, "models.json");
    }

    public void ShowConfiguration()
    {
        AnsiConsole.AlternateScreen(() =>
        {
            var exit = false;
            while (!exit)
            {
                AnsiConsole.Clear();
                AnsiConsole.Write(new Rule("[yellow]Settings[/]").RuleStyle("grey"));
                
                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select an option:")
                        .AddChoices("Models", "API Keys", "Back to Chat")
                );

                switch (choice)
                {
                    case "Models":
                        ShowModelsMenu();
                        break;
                    case "API Keys":
                        ShowApiKeysMenu();
                        break;
                    case "Back to Chat":
                        exit = true;
                        break;
                }
            }
        });
    }

    private void ShowModelsMenu()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule("[yellow]Manage Providers and Models[/]").RuleStyle("grey"));
            
            var providers = LoadProviders();
            
            var actionChoice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What would you like to configure?")
                    .AddChoices("Enable/Disable Providers", "Enable/Disable Specific Models", "[bold].. Back[/]")
            );

            if (actionChoice == "[bold].. Back[/]") break;

            if (actionChoice == "Enable/Disable Providers")
            {
                var providerChoices = providers.Select(p => new ProviderChoice(p.Name, _credentialStorage.IsProviderEnabled(p.Name, p.Enabled))).ToList();
                var prompt = new MultiSelectionPrompt<ProviderChoice>()
                    .Title("Toggle [green]Providers[/]:")
                    .PageSize(10)
                    .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to accept)[/]")
                    .AddChoices(providerChoices);

                foreach (var pc in providerChoices.Where(p => p.IsEnabled)) prompt.Select(pc);

                var selectedProviders = AnsiConsole.Prompt(prompt);
                foreach (var pc in providerChoices)
                {
                    _credentialStorage.SetProviderEnabled(pc.Name, selectedProviders.Any(sp => sp.Name == pc.Name));
                }
                AnsiConsole.MarkupLine("[green]Provider settings updated![/]");
                Thread.Sleep(800);
            }
            else if (actionChoice == "Enable/Disable Specific Models")
            {
                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select a [green]Provider[/] to manage models:")
                        .PageSize(10)
                        .AddChoices(providers.Select(p => p.Name).Concat(new[] { "[bold].. Back[/]" }))
                );

                if (choice == "[bold].. Back[/]") continue;

                var provider = providers.First(p => p.Name == choice);
                var modelChoices = provider.Models.Select(m => new ModelChoice(m.ModelId, m.ModelName, _credentialStorage.IsModelEnabled(provider.Name, m.ModelId, m.Enabled))).ToList();

                var prompt = new MultiSelectionPrompt<ModelChoice>()
                    .Title($"Enable/Disable models for [blue]{provider.Name}[/]:")
                    .PageSize(10)
                    .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to accept)[/]")
                    .AddChoices(modelChoices);

                // FIXED: MultiSelectionPrompt needs the EXACT object reference from the choice list
                foreach (var mc in modelChoices.Where(m => m.IsEnabled)) prompt.Select(mc);

                var selectedModels = AnsiConsole.Prompt(prompt);

                foreach (var mc in modelChoices)
                {
                    _credentialStorage.SetModelEnabled(provider.Name, mc.Id, selectedModels.Any(sm => sm.Id == mc.Id));
                }

                AnsiConsole.MarkupLine("[green]Model settings updated![/]");
                Thread.Sleep(800);
            }
        }
    }

    private class ProviderChoice
    {
        public string Name { get; }
        public bool IsEnabled { get; }
        public ProviderChoice(string name, bool isEnabled) { Name = name; IsEnabled = isEnabled; }
        public override string ToString() => Name;
    }

    private class ModelChoice
    {
        public string Id { get; }
        public string Name { get; }
        public bool IsEnabled { get; }

        public ModelChoice(string id, string name, bool isEnabled)
        {
            Id = id;
            Name = name;
            IsEnabled = isEnabled;
        }

        public override string ToString() => $"{Name} ({Id})";
    }

    private void ShowApiKeysMenu()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule("[yellow]Manage API Keys[/]").RuleStyle("grey"));
            
            var providers = LoadProviders();
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a [green]Provider[/] to manage keys:")
                    .PageSize(10)
                    .AddChoices(providers.Select(p => p.Name).Concat(new[] { "[bold].. Back[/]" }))
            );

            if (choice == "[bold].. Back[/]") break;

            ConfigureApiKey(choice);
        }
    }

    private void ConfigureApiKey(string providerName)
    {
        var exit = false;
        while (!exit)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[yellow]Key Management: {providerName}[/]").RuleStyle("grey"));
            
            var currentKey = _credentialStorage.GetApiKey(providerName);
            if (!string.IsNullOrEmpty(currentKey))
            {
                var maskedKey = currentKey.Length > 8 
                    ? currentKey[..4] + "..." + currentKey[^4..] 
                    : "****";
                AnsiConsole.MarkupLine($"Current Key: [blue]{maskedKey}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("Current Key: [red]Not Set[/]");
            }
            AnsiConsole.WriteLine();

            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Choose an action:")
                    .AddChoices("Update Key", "Delete Key", "Back")
            );

            switch (action)
            {
                case "Update Key":
                    var newKey = AnsiConsole.Prompt(
                        new TextPrompt<string>("Enter API Key:")
                            .PromptStyle("green")
                            .Secret()
                    );
                    if (!string.IsNullOrWhiteSpace(newKey))
                    {
                        _credentialStorage.SaveApiKey(providerName, newKey);
                        AnsiConsole.MarkupLine("[green]Key updated![/]");
                        Thread.Sleep(800);
                    }
                    break;
                case "Delete Key":
                    if (AnsiConsole.Confirm($"Are you sure you want to delete the key for [red]{providerName}[/]?"))
                    {
                        _credentialStorage.DeleteApiKey(providerName);
                        AnsiConsole.MarkupLine("[yellow]Key deleted.[/]");
                        Thread.Sleep(800);
                    }
                    break;
                case "Back":
                    exit = true;
                    break;
            }
        }
    }

    private List<MullaiProviderDescriptor> LoadProviders()
    {
        if (!File.Exists(_modelsJsonPath)) return new List<MullaiProviderDescriptor>();

        try
        {
            var json = File.ReadAllText(_modelsJsonPath);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("MullaiProviders", out var providersElement))
            {
                var config = JsonSerializer.Deserialize<MullaiProvidersConfig>(providersElement, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return config?.Providers ?? new List<MullaiProviderDescriptor>();
            }
        }
        catch
        {
            // Ignore
        }

        return new List<MullaiProviderDescriptor>();
    }
}
