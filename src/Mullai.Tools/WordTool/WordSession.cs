using DocumentFormat.OpenXml.Packaging;

namespace Mullai.Tools.WordTool;

public class WordSession
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    public string FilePath { get; set; } = default!;
    public WordprocessingDocument Document { get; set; } = default!;
    public MainDocumentPart MainPart { get; set; } = default!;
}