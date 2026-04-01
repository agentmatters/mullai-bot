using System.Text;
using Mullai.Tools.RestApiTool.Models;

namespace Mullai.Tools.RestApiTool;

public class RestApiProvider(HttpClient httpClient)
{
    public virtual async Task<RestApiResponse> SendAsync(RestApiRequest request)
    {
        try
        {
            var httpRequest = new HttpRequestMessage(new HttpMethod(request.Method), request.Url);

            if (request.Headers != null)
                foreach (var header in request.Headers)
                {
                    if (header.Key.Equals("Content-Type",
                            StringComparison.OrdinalIgnoreCase)) continue; // Set via content
                    httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

            if (!string.IsNullOrEmpty(request.Body))
                httpRequest.Content =
                    new StringContent(request.Body, Encoding.UTF8, request.ContentType ?? "application/json");

            var response = await httpClient.SendAsync(httpRequest);
            var content = await response.Content.ReadAsStringAsync();

            var result = new RestApiResponse
            {
                StatusCode = response.StatusCode,
                IsSuccess = response.IsSuccessStatusCode,
                Content = content,
                Headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value))
            };

            return result;
        }
        catch (Exception ex)
        {
            return new RestApiResponse
            {
                IsSuccess = false,
                Error = ex.Message
            };
        }
    }
}