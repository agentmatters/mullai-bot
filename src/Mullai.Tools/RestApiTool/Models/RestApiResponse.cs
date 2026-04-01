using System.Net;

namespace Mullai.Tools.RestApiTool.Models;

public class RestApiResponse
{
    public HttpStatusCode StatusCode { get; set; }
    public string? Content { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public bool IsSuccess { get; set; }
    public string? Error { get; set; }
}