using Mullai.Tools.WordTool;

namespace Mullai.Tools.Tests.WordTool;

public class WordToolTests
{
    [Fact]
    public async Task WordTool_FullFlow_Success()
    {
        var provider = new WordProvider();
        var tool = new Tools.WordTool.WordTool(provider);
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.docx");

        try
        {
            // 1. Create session
            var sessionId = await tool.CreateWordSession();
            Assert.NotNull(sessionId);

            // 2. Open / Create document
            var openResult = await tool.OpenWordDocument(sessionId, filePath);
            Assert.Equal("Document opened successfully.", openResult);

            // 3. Add Heading
            var headingResult = await tool.AddWordFormattedText(sessionId, "Test Report", true, headingLevel: 1);
            Assert.Equal("Formatted text added.", headingResult);

            // 4. Append Text
            var appendResult = await tool.AppendWordText(sessionId, "This is a test paragraph.");
            Assert.Equal("Text appended.", appendResult);

            // 5. Insert Table
            var tableResult = await tool.InsertWordTable(sessionId, 2, 2, "Cell");
            Assert.Equal("Table inserted.", tableResult);

            // 6. Read Document (Basic check)
            var readResult = await tool.ReadWordDocument(sessionId);
            Assert.Contains("Test Report", readResult);
            Assert.Contains("This is a test paragraph.", readResult);

            // 7. Replace Text
            var replaceResult = await tool.ReplaceWordText(sessionId, "test", "verified test");
            Assert.Equal("Text replaced.", replaceResult);

            // 8. Close session
            var closeResult = await tool.CloseWordSession(sessionId);
            Assert.Equal("Session closed and document saved.", closeResult);

            // 9. Verify file exists
            Assert.True(File.Exists(filePath));
        }
        finally
        {
            if (File.Exists(filePath)) File.Delete(filePath);
        }
    }
}