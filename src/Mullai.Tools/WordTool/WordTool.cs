using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace Mullai.Tools.WordTool;

/// <summary>
///     The agent plugin that provides Word document manipulation capabilities using OpenXML.
///     This tool uses a session-based model to maintain state across multiple operations on a single document.
///     Typical Usage Flow:
///     1. CreateWordSession() - Obtain a unique session ID.
///     2. OpenWordDocument(sessionId, filePath) - Open an existing .docx or create a new one.
///     3. Perform edits (AppendWordText, AddWordFormattedText, InsertWordTable, ReplaceWordText).
///     4. ReadWordDocument(sessionId) - Optional verification of content.
///     5. CloseWordSession(sessionId) - CRITICAL: Call this to save changes and release file locks.
/// </summary>
[Description(
    "A session-based Word document editor for creating and modifying .docx files. TO USE: 1) Call CreateWordSession to get a session ID. 2) Call OpenWordDocument with the ID and a file path. 3) Use editing methods (AppendWordText, AddWordFormattedText, etc.). 4) IMPORTANT: Call CloseWordSession to save changes and release the file handle.")]
public class WordTool(WordProvider wordProvider)
{
    // ---------------------------
    // SESSION MANAGEMENT
    // ---------------------------

    /// <summary>
    ///     Creates a new Word editing session.
    /// </summary>
    /// <returns>A unique Word session ID string.</returns>
    [Description(
        "Creates a new Word editing session and returns a unique session ID. Use this ID for all subsequent Word operations to maintain document state.")]
    public Task<string> CreateWordSession()
    {
        return Task.FromResult(wordProvider.CreateSession());
    }

    /// <summary>
    ///     Closes a Word session and saves the document.
    /// </summary>
    /// <param name="sessionId">The unique Word session ID.</param>
    /// <returns>A confirmation message.</returns>
    [Description(
        "Closes an active Word session, saves all pending changes to the document, and releases the file handle for other applications to use.")]
    public Task<string> CloseWordSession(
        [Description("The unique ID of the Word session to close.")]
        string sessionId)
    {
        return Task.FromResult(wordProvider.CloseSession(sessionId));
    }

    // ---------------------------
    // FILE OPERATIONS
    // ---------------------------

    /// <summary>
    ///     Creates or opens a Word document.
    /// </summary>
    /// <param name="sessionId">The Word session ID.</param>
    /// <param name="filePath">The path to the .docx file.</param>
    /// <returns>A success or error message.</returns>
    [Description(
        "Opens an existing Word (.docx) document or creates a new one at the specified path within an active session.")]
    public Task<string> OpenWordDocument(
        [Description("The unique ID of the Word session.")]
        string sessionId,
        [Description("The absolute or relative path to the Word (.docx) file.")]
        string filePath)
    {
        return Task.FromResult(wordProvider.OpenDocument(sessionId, filePath));
    }

    /// <summary>
    ///     Reads all text from the Word document.
    /// </summary>
    /// <param name="sessionId">The Word session ID.</param>
    /// <returns>All text content from the document body.</returns>
    [Description("Reads and returns all plain text content from the body of the Word document in the current session.")]
    public Task<string> ReadWordDocument(
        [Description("The unique ID of the Word session.")]
        string sessionId)
    {
        return Task.FromResult(wordProvider.ReadDocument(sessionId));
    }

    // ---------------------------
    // TEXT OPERATIONS
    // ---------------------------

    /// <summary>
    ///     Appends plain text to the Word document.
    /// </summary>
    /// <param name="sessionId">The Word session ID.</param>
    /// <param name="text">The text to append.</param>
    /// <returns>A confirmation message.</returns>
    [Description("Appends a new paragraph containing the specified plain text to the end of the Word document.")]
    public Task<string> AppendWordText(
        [Description("The unique ID of the Word session.")]
        string sessionId,
        [Description("The plain text to append as a new paragraph.")]
        string text)
    {
        return Task.FromResult(wordProvider.AppendText(sessionId, text));
    }

    /// <summary>
    ///     Adds formatted text (bold, italic, heading) to the Word document.
    /// </summary>
    /// <param name="sessionId">The Word session ID.</param>
    /// <param name="text">The text content.</param>
    /// <param name="bold">True for bold text.</param>
    /// <param name="italic">True for italic text.</param>
    /// <param name="headingLevel">The heading level (1-6), or 0 for normal text.</param>
    /// <returns>A confirmation message.</returns>
    [Description("Adds a new paragraph with optional styling such as bold, italic, or specific heading levels (1-6).")]
    public Task<string> AddWordFormattedText(
        [Description("The unique ID of the Word session.")]
        string sessionId,
        [Description("The text content to add.")]
        string text,
        [Description("Set to true to make the text bold.")]
        bool bold = false,
        [Description("Set to true to make the text italic.")]
        bool italic = false,
        [Description(
            "Specify heading level (e.g., 1 for main title, 2 for subtitle). Set to 0 for normal paragraph text.")]
        int headingLevel = 0)
    {
        return Task.FromResult(wordProvider.AddFormattedText(sessionId, text, bold, italic, headingLevel));
    }

    /// <summary>
    ///     Replaces all occurrences of a text in the Word document.
    /// </summary>
    /// <param name="sessionId">The Word session ID.</param>
    /// <param name="find">The text to find.</param>
    /// <param name="replace">The replacement text.</param>
    /// <returns>A confirmation message.</returns>
    [Description("Finds and replaces all occurrences of a specific string throughout the Word document's body text.")]
    public Task<string> ReplaceWordText(
        [Description("The unique ID of the Word session.")]
        string sessionId,
        [Description("The exact text string to search for.")]
        string find,
        [Description("The text string to replace the found text with.")]
        string replace)
    {
        return Task.FromResult(wordProvider.ReplaceText(sessionId, find, replace));
    }

    // ---------------------------
    // TABLE OPERATIONS
    // ---------------------------

    /// <summary>
    ///     Inserts a table into the Word document.
    /// </summary>
    /// <param name="sessionId">The Word session ID.</param>
    /// <param name="rows">Number of rows.</param>
    /// <param name="cols">Number of columns.</param>
    /// <param name="defaultText">Default text for each cell.</param>
    /// <returns>A confirmation message.</returns>
    [Description("Inserts a grid-like table into the Word document with the specified number of rows and columns.")]
    public Task<string> InsertWordTable(
        [Description("The unique ID of the Word session.")]
        string sessionId,
        [Description("The total number of rows for the table.")]
        int rows,
        [Description("The total number of columns for the table.")]
        int cols,
        [Description("Optional: The default text content for every newly created cell.")]
        string? defaultText = "")
    {
        return Task.FromResult(wordProvider.InsertTable(sessionId, rows, cols, defaultText));
    }

    /// <summary>
    ///     Returns the functions provided by this plugin for tool calling.
    /// </summary>
    /// <returns>An enumerable of AI tools.</returns>
    public IEnumerable<AITool> AsAITools()
    {
        yield return AIFunctionFactory.Create(CreateWordSession);
        yield return AIFunctionFactory.Create(CloseWordSession);

        yield return AIFunctionFactory.Create(OpenWordDocument);
        yield return AIFunctionFactory.Create(ReadWordDocument);

        yield return AIFunctionFactory.Create(AppendWordText);
        yield return AIFunctionFactory.Create(AddWordFormattedText);
        yield return AIFunctionFactory.Create(ReplaceWordText);

        yield return AIFunctionFactory.Create(InsertWordTable);
    }
}