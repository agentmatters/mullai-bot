using Microsoft.Extensions.DependencyInjection;
using Mullai.Agents;
using Mullai.TUI.Spectre.Controllers;
using Mullai.TUI.TUI.State;
using Mullai.Abstractions.Observability;
using Mullai.Middleware.Middlewares;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Mullai.TUI.Spectre;

public class MullaiSpectreApp
{
    private readonly IServiceProvider _services;
    private readonly ChatState _state;
    private readonly SpectreChatController _controller;

    public MullaiSpectreApp(IServiceProvider services)
    {
        _services = services;
        _state = new ChatState();
        var agentFactory = _services.GetRequiredService<AgentFactory>();
        _controller = new SpectreChatController(agentFactory, _state);

        // Wire the FunctionCallingMiddleware to emit tool call observations
        // into the singleton channel.
        var middleware = _services.GetRequiredService<FunctionCallingMiddleware>();
        middleware.OnToolCallObserved = obs => ToolCallChannel.Instance.Writer.TryWrite(obs);
    }

    public async Task RunAsync()
    {
        await _controller.InitialiseAsync();

        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[yellow]Mullai - AI Chat Console[/]").RuleStyle("grey").Justify(Justify.Left));
        AnsiConsole.MarkupLine("[grey]Type [bold white]/quit[/] to exit.[/]");
        AnsiConsole.WriteLine();

        while (true)
        {
            var input = AnsiConsole.Prompt(
                new TextPrompt<string>("You: ")
                    .PromptStyle("green")
            );

            if (string.IsNullOrWhiteSpace(input)) continue;
            if (input.Equals("/quit", StringComparison.OrdinalIgnoreCase)) break;

            // Capture the exact moment the turn starts (before adding user message to state)
            var turnStart = DateTimeOffset.Now;

            // Handle the message via controller (which adds to state)
            // But we render the user's part manually first to have it "fixed" above the live region
            AnsiConsole.Write(RenderEntry(new ChatMessage(input, true, turnStart)));
            AnsiConsole.WriteLine();

            // Now handle the agent response with a Live display for everything that follows
            await AnsiConsole.Live(new Markup("[grey]Mullai is thinking...[/]"))
                .AutoClear(false)
                .StartAsync(async ctx =>
                {
                    Action updateHandler = () => ctx.UpdateTarget(RenderTurnEntries(turnStart));
                    _state.StateChanged += updateHandler;
                    
                    try 
                    {
                        await _controller.HandleMessageAsync(input);
                    }
                    finally
                    {
                        _state.StateChanged -= updateHandler;
                        // Final update to ensure everything is rendered
                        ctx.UpdateTarget(RenderTurnEntries(turnStart));
                    }
                });
            
            AnsiConsole.WriteLine();
        }
        
        AnsiConsole.MarkupLine("[yellow]Goodbye![/]");
    }

    private IRenderable RenderEntry(object entry)
    {
        if (entry is ChatMessage msg)
        {
            var content = msg.IsUser ? msg.Content : ProcessHighlights(msg.Content);
            return new Panel(content)
                .Header(msg.IsUser ? "[green]You[/]" : "[blue]Mullai[/]", msg.IsUser ? Justify.Right : Justify.Left)
                .Border(BoxBorder.Rounded)
                .BorderStyle(msg.IsUser ? "green" : "blue")
                .Expand();
        }
        else if (entry is ToolCallObservation obs)
        {
            var table = new Table().Border(TableBorder.Rounded).BorderStyle("grey").Expand();
            table.AddColumn(new TableColumn("[cyan]Tool Call[/]").Centered());
            table.AddRow($"[yellow]Tool:[/] {obs.ToolName}");
            var argsJson = System.Text.Json.JsonSerializer.Serialize(obs.Arguments, new System.Text.Json.JsonSerializerOptions { WriteIndented = false });
            table.AddRow($"[yellow]Arguments:[/] [grey]{argsJson}[/]");
            if (!string.IsNullOrEmpty(obs.Result))
            {
                table.AddRow($"[yellow]Result:[/] {obs.Result}");
            }
            if (!string.IsNullOrEmpty(obs.Error))
            {
                table.AddRow($"[red]Error:[/] {obs.Error}");
            }
            return table;
        }

        return new Markup(string.Empty);
    }

    private string ProcessHighlights(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        
        // Escape existing brackets to prevent Spectre from misinterpreting them
        var escaped = Markup.Escape(text);
        
        // Replace **text** with [bold yellow]text[/]
        // Note: Regex.Replace on escaped string. 
        // We need to be careful if the original text had [ ] that were escaped.
        return System.Text.RegularExpressions.Regex.Replace(escaped, @"\*\*(.*?)\*\*", "[bold yellow]$1[/]");
    }

    private IRenderable RenderTurnEntries(DateTimeOffset turnStart)
    {
        var entries = _state.ChronologicalEntries.Where(e => e switch {
            ChatMessage m => !m.IsUser && m.Timestamp > turnStart,
            ToolCallObservation t => t.StartedAt > turnStart,
            _ => false
        }).ToList();

        if (entries.Count == 0 && _state.IsThinking)
        {
            return new Markup("[grey]Mullai is thinking...[/]");
        }

        if (entries.Count == 0)
        {
            return new Markup(string.Empty);
        }

        return new Rows(entries.Select(RenderEntry));
    }
}
