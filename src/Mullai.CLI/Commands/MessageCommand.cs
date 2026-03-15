using Mullai.CLI.Controllers;
using Mullai.CLI.State;
using Mullai.CLI.UI;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Mullai.CLI.Commands;

public class MessageCommand : ICommand
{
    private readonly string _input;
    private readonly ChatOrchestrator _controller;
    private readonly ChatState _state;

    public MessageCommand(string input, ChatOrchestrator controller, ChatState state)
    {
        _input = input;
        _controller = controller;
        _state = state;
    }

    public async Task ExecuteAsync()
    {
        var turnStart = DateTimeOffset.Now;

        // Render user message panel
        var userMessage = new ChatMessage(_input, true, turnStart);
        // Create a helper to provide the turn-specific renderable
        Func<IRenderable> renderTurn = () => new Rows(
            RenderTurnHistory(turnStart),
            new Rule().RuleStyle("grey"),
            RenderStatsPanel()
        );

        // Handle agent response with live display
        await AnsiConsole.Live(renderTurn())
            .Overflow(VerticalOverflow.Crop)
            .Cropping(VerticalOverflowCropping.Top)
            .AutoClear(true)
            .StartAsync(async ctx =>
            {
                Action updateHandler = () => {
                    ctx.UpdateTarget(renderTurn());
                    ctx.Refresh();
                };
                _state.StateChanged += updateHandler;
                
                try 
                {
                    await _controller.HandleMessageAsync(_input);
                }
                finally
                {
                    _state.StateChanged -= updateHandler;
                    ctx.UpdateTarget(renderTurn());
                    ctx.Refresh();
                }
            });
        
        // Final persistent render to terminal
        AnsiConsole.Write(renderTurn());
        AnsiConsole.WriteLine();
    }

    private IRenderable RenderTurnHistory(DateTimeOffset turnStart)
    {
        var entries = _state.ChronologicalEntries.Where(e => e switch {
            ChatMessage m => !m.IsUser && m.Timestamp > turnStart,
            Mullai.Abstractions.Observability.ToolCallObservation t => t.StartedAt > turnStart,
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

        var content = entries.Select(e => e switch {
            ChatMessage m => new ChatMessageComponent(m).Render(),
            Mullai.Abstractions.Observability.ToolCallObservation t => new ToolCallComponent(t).Render(),
            _ => new Markup(string.Empty)
        }).ToList();

        return new Rows(content);
    }

    private IRenderable RenderStatsPanel()
    {
        var grid = new Grid();
        grid.AddColumn(new GridColumn().NoWrap());
        grid.AddColumn(new GridColumn().Padding(2, 0));
        grid.AddColumn(new GridColumn().Padding(2, 0));

        grid.AddRow(
            new Markup($"[bold cyan]Model:[/] GPT-4o"),
            new Markup($"[bold cyan]Provider:[/] OpenAI"),
            new Markup($"[bold cyan]Stats:[/] [green]1250 tks[/] | [yellow]450ms[/]")
        );

        return grid;
        // return new Panel(grid)
        //     .Header("[bold yellow] Live Stats [/]")
        //     .BorderColor(Color.Grey)
        //     .RoundedBorder();
    }
}
