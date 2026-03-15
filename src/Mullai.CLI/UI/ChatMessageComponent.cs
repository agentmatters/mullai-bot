using Mullai.CLI.State;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Mullai.CLI.UI;

public class ChatMessageComponent : IRenderableComponent
{
    private readonly ChatMessage _message;

    public ChatMessageComponent(ChatMessage message)
    {
        _message = message;
    }

    public IRenderable Render()
    {
        var content = _message.IsUser ? _message.Content : ProcessHighlights(_message.Content);
        return new Panel(content)
            .Header(_message.IsUser ? "[green]You[/]" : "[blue]Mullai[/]", _message.IsUser ? Justify.Right : Justify.Left)
            .Border(_message.IsUser ? BoxBorder.Rounded : BoxBorder.None)
            .BorderStyle(_message.IsUser ? "green" : "blue");
    }

    private string ProcessHighlights(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        
        var escaped = Markup.Escape(text);
        return System.Text.RegularExpressions.Regex.Replace(escaped, @"\*\*(.*?)\*\*", "[bold yellow]$1[/]");
    }
}
