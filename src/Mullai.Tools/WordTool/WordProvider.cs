using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Mullai.Tools.WordTool;

/// <summary>
///     A provider for Word document operations.
/// </summary>
public class WordProvider : IDisposable
{
    private readonly Dictionary<string, WordSession> _sessions = new();

    public void Dispose()
    {
        foreach (var session in _sessions.Values)
            try
            {
                session.Document.Dispose();
            }
            catch
            {
            }

        _sessions.Clear();
    }

    /// <summary>
    ///     Creates a new Word editing session.
    /// </summary>
    public string CreateSession()
    {
        var session = new WordSession();
        _sessions[session.SessionId] = session;
        return session.SessionId;
    }

    /// <summary>
    ///     Closes a Word session and saves the document.
    /// </summary>
    public string CloseSession(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return "Session not found";

        session.Document.Save();
        session.Document.Dispose();
        _sessions.Remove(sessionId);

        return "Session closed and document saved.";
    }

    /// <summary>
    ///     Creates or opens a Word document.
    /// </summary>
    public string OpenDocument(string sessionId, string filePath)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return "Session not found";

        WordprocessingDocument doc;

        if (File.Exists(filePath))
        {
            doc = WordprocessingDocument.Open(filePath, true);
        }
        else
        {
            doc = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());
        }

        session.Document = doc;
        session.MainPart = doc.MainDocumentPart!;
        session.FilePath = filePath;

        return "Document opened successfully.";
    }

    /// <summary>
    ///     Reads all text from the Word document.
    /// </summary>
    public string ReadDocument(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return "Session not found";

        var body = session.MainPart.Document.Body;
        var text = body?.InnerText ?? string.Empty;

        return text;
    }

    /// <summary>
    ///     Appends plain text to the Word document.
    /// </summary>
    public string AppendText(string sessionId, string text)
    {
        var session = GetSession(sessionId);

        var para = new Paragraph(new Run(new Text(text)));
        session.MainPart.Document.Body!.AppendChild(para);

        return "Text appended.";
    }

    /// <summary>
    ///     Adds formatted text (bold, italic, heading) to the Word document.
    /// </summary>
    public string AddFormattedText(string sessionId, string text, bool bold = false, bool italic = false,
        int headingLevel = 0)
    {
        var session = GetSession(sessionId);

        var runProps = new RunProperties();

        if (bold) runProps.Append(new Bold());
        if (italic) runProps.Append(new Italic());

        var run = new Run();
        run.Append(runProps);
        run.Append(new Text(text));

        var para = new Paragraph(run);

        if (headingLevel > 0)
            para.ParagraphProperties = new ParagraphProperties(
                new ParagraphStyleId { Val = $"Heading{headingLevel}" });

        session.MainPart.Document.Body!.AppendChild(para);

        return "Formatted text added.";
    }

    /// <summary>
    ///     Replaces all occurrences of a text in the Word document.
    /// </summary>
    public string ReplaceText(string sessionId, string find, string replace)
    {
        var session = GetSession(sessionId);

        var body = session.MainPart.Document.Body!;
        foreach (var text in body.Descendants<Text>())
            if (text.Text != null && text.Text.Contains(find))
                text.Text = text.Text.Replace(find, replace);

        return "Text replaced.";
    }

    /// <summary>
    ///     Inserts a table into the Word document.
    /// </summary>
    public string InsertTable(string sessionId, int rows, int cols, string? defaultText = "")
    {
        var session = GetSession(sessionId);

        var table = new Table();

        for (var i = 0; i < rows; i++)
        {
            var tr = new TableRow();

            for (var j = 0; j < cols; j++)
            {
                var tc = new TableCell(
                    new Paragraph(
                        new Run(new Text(defaultText ?? "")))
                );

                tr.Append(tc);
            }

            table.Append(tr);
        }

        session.MainPart.Document.Body!.AppendChild(table);

        return "Table inserted.";
    }

    private WordSession GetSession(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            throw new Exception("Session not found");

        return session;
    }
}