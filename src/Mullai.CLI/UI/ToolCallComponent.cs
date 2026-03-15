using Mullai.Abstractions.Observability;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Mullai.CLI.UI;

public class ToolCallComponent : IRenderableComponent
{
    private readonly ToolCallObservation _observation;

    public ToolCallComponent(ToolCallObservation observation)
    {
        _observation = observation;
    }

    public IRenderable Render()
    {
        var table = new Table().Border(TableBorder.Rounded).BorderStyle("grey");
        table.AddColumn(new TableColumn("[cyan]Tool Call[/]").Centered());
        table.AddRow($"[yellow]Tool:[/] {_observation.ToolName}");
        
        var argsJson = System.Text.Json.JsonSerializer.Serialize(
            _observation.Arguments, 
            new System.Text.Json.JsonSerializerOptions { WriteIndented = false }
        );
        table.AddRow($"[yellow]Arguments:[/] [grey]{argsJson}[/]");
        
        if (!string.IsNullOrEmpty(_observation.Result))
        {
            table.AddRow($"[yellow]Result:[/] {_observation.Result}");
        }
        
        if (!string.IsNullOrEmpty(_observation.Error))
        {
            table.AddRow($"[red]Error:[/] {_observation.Error}");
        }
        
        return table;
    }
}
