using Mullai.Abstractions.Configuration;
using Mullai.Abstractions.Models;
using Spectre.Console;

namespace Mullai.CLI.UI;

using Mullai.CLI.Controllers;

public class ConfigView
{
    private readonly ConfigController _controller;
    private const string GoBack = "[bold].. Back[/] ('Esc')";
    
    public ConfigView(ConfigController controller)
    {
        _controller = controller;
    }

    public void Show()
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
                        .AddChoices("Models", "API Keys", "Back to Chat ('Esc')")
                        .AddCancelResult(()=>"Back to Chat ('Esc')")
                );

                switch (choice)
                {
                    case "Models":
                        ShowModelsMenu();
                        break;
                    case "API Keys":
                        ShowApiKeysMenu();
                        break;
                    case "Back to Chat ('Esc')":
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
            
            var providers = _controller.LoadProviders();
            
            var actionChoice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What would you like to configure?")
                    .AddChoices("Enable/Disable Providers", "Enable/Disable Specific Models", "Disable All Models", "Refresh/Fetch Models from Provider", GoBack)
                    .AddCancelResult(()=>GoBack)
            );

            if (actionChoice == GoBack) break;

            if (actionChoice == "Enable/Disable Providers")
            {
                if (providers.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No providers available to configure.[/]");
                    Thread.Sleep(1000);
                    continue;
                }
                var providerChoices = providers.Select(p => new ProviderChoice(p.Name, _controller.IsProviderEnabled(p.Name, p.Enabled))).ToList();
                var prompt = new MultiSelectionPrompt<ProviderChoice>()
                    .Title("Toggle [green]Providers[/]:")
                    .PageSize(10)
                    .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to accept)[/]")
                    .AddChoices(providerChoices);

                foreach (var pc in providerChoices.Where(p => p.IsEnabled)) prompt.Select(pc);

                var selectedProviders = AnsiConsole.Prompt(prompt);
                foreach (var pc in providerChoices)
                {
                    _controller.SetProviderEnabled(pc.Name, selectedProviders.Any(sp => sp.Name == pc.Name));
                }
                AnsiConsole.MarkupLine("[green]Provider settings updated![/]");
                Thread.Sleep(800);
            }
            else if (actionChoice == "Enable/Disable Specific Models")
            {
                if (providers.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No providers available.[/]");
                    Thread.Sleep(1000);
                    continue;
                }
                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select a [green]Provider[/] to manage models:")
                        .PageSize(10)
                        .AddChoices(providers.Select(p => p.Name).Concat(new[] { GoBack }))
                        .AddCancelResult(()=>GoBack)
                );

                if (choice == GoBack) continue;

                var provider = providers.First(p => p.Name == choice);
                
                var doneSearching = false;
                while (!doneSearching)
                {
                    AnsiConsole.Clear();
                    AnsiConsole.Write(new Rule($"[blue]{provider.Name}[/] - Enable/Disable Models").RuleStyle("grey"));
                    
                    var filter = AnsiConsole.Prompt(
                        new TextPrompt<string>("Search models (leave empty for all, or '..' to go back):")
                            .AllowEmpty()
                    );

                    if (filter == "..") 
                    {
                        doneSearching = true;
                        continue;
                    }

                    var filteredModels = provider.Models;
                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        filteredModels = filteredModels.Where(m => 
                            m.ModelName.Contains(filter, StringComparison.OrdinalIgnoreCase) || 
                            m.ModelId.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
                    }

                    if (filteredModels.Count == 0)
                    {
                        AnsiConsole.MarkupLine("[yellow]No models found matching your search.[/]");
                        Thread.Sleep(1000);
                        continue;
                    }

                    var modelChoices = filteredModels.Select(m => new ModelChoice(m)).ToList();

                    var prompt = new MultiSelectionPrompt<ModelChoice>()
                        .Title($"Toggle models (use [blue]<space>[/] to select):")
                        .PageSize(15)
                        .InstructionsText("[grey]([green]<enter>[/] to confirm selection for this search)[/]")
                        .AddChoices(modelChoices);

                    foreach (var mc in modelChoices.Where(m => _controller.IsModelEnabled(provider.Name, m.Id, m.Model.Enabled))) prompt.Select(mc);

                    var selectedModels = AnsiConsole.Prompt(prompt);

                    foreach (var mc in modelChoices)
                    {
                        _controller.SetModelEnabled(provider.Name, mc.Id, selectedModels.Any(sm => sm.Id == mc.Id));
                    }

                    _controller.SaveProviders();
                    AnsiConsole.MarkupLine("[green]Model settings updated for this results![/]");
                    Thread.Sleep(500);
                }
            }
            else if (actionChoice == "Disable All Models")
            {
                if (AnsiConsole.Confirm("Are you sure you want to disable [red]ALL[/] models for all providers?"))
                {
                    foreach (var p in providers)
                    {
                        foreach (var m in p.Models)
                        {
                            _controller.SetModelEnabled(p.Name, m.ModelId, false);
                        }
                    }
                    
                    var customProviders = _controller.GetCustomProviders();
                    foreach (var cp in customProviders)
                    {
                        cp.Enabled = false;
                        _controller.SaveCustomProvider(cp);
                    }

                    _controller.SaveProviders();
                    AnsiConsole.MarkupLine("[green]All models have been disabled![/]");
                    Thread.Sleep(1000);
                }
            }
            else if (actionChoice == "Refresh/Fetch Models from Provider")
            {
                if (providers.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No providers available.[/]");
                    Thread.Sleep(1000);
                    continue;
                }
                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select a [green]Provider[/] to refresh models:")
                        .PageSize(10)
                        .AddChoices(providers.Select(p => p.Name).Concat(new[] { GoBack }))
                        .AddCancelResult(()=>GoBack)
                );

                if (choice == GoBack) continue;

                var provider = providers.First(p => p.Name == choice);
                
                AnsiConsole.Status()
                    .Start($"Fetching models for [blue]{provider.Name}[/]...", ctx =>
                    {
                        var task = _controller.GetModelsAsync(provider.Name);
                        provider.Models = task.GetAwaiter().GetResult();
                    });

                _controller.SaveProviders();
                AnsiConsole.MarkupLine($"[green]Successfully fetched {provider.Models.Count} models for {provider.Name}![/]");
                
                // Show a quick table of fetched models
                var table = new Table().Border(TableBorder.Rounded);
                table.AddColumn("Name");
                table.AddColumn("Context");
                table.AddColumn("Pricing (In/Out per 1k)");
                
                foreach(var m in provider.Models.Take(10))
                {
                    var pricing = m.Pricing != null 
                        ? $"${m.Pricing.InputPer1kTokens:F6} / ${m.Pricing.OutputPer1kTokens:F6}" 
                        : "Free/Unknown";
                    table.AddRow(m.ModelName, $"{m.ContextWindow:N0}", pricing);
                }
                
                if (provider.Models.Count > 10) table.Caption($"... and {provider.Models.Count - 10} more");
                
                AnsiConsole.Write(table);
                AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
                Console.ReadKey(true);
            }

        }
    }

    private void ShowApiKeysMenu()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule("[yellow]Manage API Keys[/]").RuleStyle("grey"));
            
            var providers = _controller.LoadProviders();
            if (providers.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No providers available.[/]");
                Thread.Sleep(1000);
                break;
            }
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a [green]Provider[/] to manage keys:")
                    .PageSize(10)
                    .AddChoices(providers.Select(p => p.Name).Concat(new[] { GoBack }))
                    .AddCancelResult(()=>GoBack)
            );

            if (choice == GoBack) break;

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
            
            var currentKey = _controller.GetApiKey(providerName);
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
                    .AddChoices("Update Key", "Delete Key", GoBack)
                    .AddCancelResult(()=>GoBack)
            );

            switch (action)
            {
                case "Update Key":
                    var newKey = AnsiConsole.Prompt(
                        new TextPrompt<string>("Enter API Key:")
                            .PromptStyle("green")
                            .Secret()
                            .AllowEmpty()
                    );
                    if (!string.IsNullOrWhiteSpace(newKey))
                    {
                        _controller.SaveApiKey(providerName, newKey);
                        AnsiConsole.MarkupLine("[green]Key updated![/]");
                        Thread.Sleep(800);
                    }
                    break;
                case "Delete Key":
                    if (AnsiConsole.Confirm($"Are you sure you want to delete the key for [red]{providerName}[/]?"))
                    {
                        _controller.DeleteApiKey(providerName);
                        AnsiConsole.MarkupLine("[yellow]Key deleted.[/]");
                        Thread.Sleep(800);
                    }
                    break;
                case GoBack:
                    exit = true;
                    break;
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
        public string Id => Model.ModelId;
        public string Name => Model.ModelName;
        public MullaiModelDescriptor Model { get; }

        public ModelChoice(MullaiModelDescriptor model)
        {
            Model = model;
        }

        public override string ToString()
        {
            var pricing = Model.Pricing != null 
                ? $"(${Model.Pricing.InputPer1kTokens:F6}/${Model.Pricing.OutputPer1kTokens:F6})" 
                : "(Free)";
            return $"{Name} [grey]({Id})[/] | [blue]{Model.ContextWindow:N0}[/] | [yellow]{pricing}[/]";
        }
    }

}
