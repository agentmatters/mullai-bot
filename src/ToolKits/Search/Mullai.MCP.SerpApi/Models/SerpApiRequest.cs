namespace Mullai.MCP.SerpApi.Models
{
    public class SerpApiRequest
    {
        public string Query { get; set; }
        = string.Empty;

        public string Location { get; set; }
            = string.Empty;

        public string Language { get; set; }
            = "en";

        public int NumResults { get; set; }
            = 10;
    }
}