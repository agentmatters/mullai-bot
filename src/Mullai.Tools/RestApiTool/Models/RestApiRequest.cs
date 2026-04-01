namespace Mullai.Tools.RestApiTool.Models;

public class RestApiRequest
{
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public Dictionary<string, string>? Headers { get; set; }
    public string? Body { get; set; }
    public string? ContentType { get; set; } = "application/json";
}