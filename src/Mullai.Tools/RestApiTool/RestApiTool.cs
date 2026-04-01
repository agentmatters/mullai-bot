using System.ComponentModel;
using Microsoft.Extensions.AI;
using Mullai.Tools.RestApiTool.Models;

namespace Mullai.Tools.RestApiTool;

/// <summary>
///     A tool for making REST API requests to any endpoint.
/// </summary>
[Description(
    "A versatile tool for making HTTP REST API requests (GET, POST, PUT, DELETE, etc.) to any web service or endpoint.")]
public class RestApiTool(RestApiProvider restApiProvider)
{
    /// <summary>
    ///     Sends a REST API request to the specified URL.
    /// </summary>
    /// <param name="url">The full URL of the API endpoint.</param>
    /// <param name="method">The HTTP method to use (GET, POST, PUT, DELETE, PATCH, etc.). Default is GET.</param>
    /// <param name="body">The request body content (for POST, PUT, PATCH). Leave empty for GET/DELETE.</param>
    /// <param name="contentType">
    ///     The media type of the body (e.g., 'application/json', 'text/plain'). Default is
    ///     'application/json'.
    /// </param>
    /// <param name="headers">Optional dictionary of HTTP headers to include in the request.</param>
    /// <returns>A detailed response including status code, content, and headers.</returns>
    [Description("Sends an HTTP request to a specified URL and returns the response details.")]
    public async Task<RestApiResponse> SendRequestAsync(
        [Description("The complete URL of the endpoint (e.g., 'https://api.example.com/v1/data').")]
        string url,
        [Description("The HTTP verb to use (GET, POST, PUT, DELETE, etc.). Defaults to GET.")]
        string method = "GET",
        [Description("The payload to send with the request, typically a JSON string.")]
        string? body = null,
        [Description("The Content-Type header for the request body.")]
        string? contentType = "application/json",
        [Description("A key-value pair of additional headers to send.")]
        Dictionary<string, string>? headers = null)
    {
        var request = new RestApiRequest
        {
            Url = url,
            Method = method,
            Body = body,
            ContentType = contentType,
            Headers = headers
        };

        return await restApiProvider.SendAsync(request);
    }

    /// <summary>
    ///     Returns the functions provided by this plugin.
    /// </summary>
    /// <returns>The functions provided by this plugin.</returns>
    public IEnumerable<AITool> AsAITools()
    {
        yield return AIFunctionFactory.Create(SendRequestAsync);
    }
}