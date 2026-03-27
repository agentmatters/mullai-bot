using Microsoft.Extensions.Logging;

namespace Mullai.Logging.LLMRequestLogging;

public class LLMRequestLoggingHandler : DelegatingHandler
{
    private readonly ILogger<LLMRequestLoggingHandler> _logger;
    
    public LLMRequestLoggingHandler(ILogger<LLMRequestLoggingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Request: {Method} {Uri}", request.Method, request.RequestUri);
        
        // Log request payload
        if (request.Content != null && _logger.IsEnabled(LogLevel.Debug))
        {
            using var stream = new MemoryStream();
            await request.Content.CopyToAsync(stream, null, cancellationToken);
            var requestContent = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            _logger.LogDebug("Request Payload: {Payload}", requestContent);
                
            // Recreate the content so it can be used by the downstream handler
            var encoding = GetEncodingFromHeader(request.Content.Headers.ContentEncoding.FirstOrDefault());
            var mediaType = request.Content.Headers.ContentType?.MediaType;
            request.Content = new StringContent(requestContent, encoding, mediaType);
        }
        
        var response = await base.SendAsync(request, cancellationToken);
        
        _logger.LogInformation("Response: {StatusCode}", response.StatusCode);
        
        // Log response payload
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            using var stream = new MemoryStream();
            await response.Content.CopyToAsync(stream, null, cancellationToken);
            var responseContent = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            _logger.LogDebug("Response Payload: {Payload}", responseContent);
                
            // Recreate the content so it can be consumed by the caller
            var encoding = GetEncodingFromHeader(response.Content.Headers.ContentEncoding.FirstOrDefault());
            var mediaType = response.Content.Headers.ContentType?.MediaType;
            response.Content = new StringContent(responseContent, encoding, mediaType);
        }
        
        return response;
    }
    
    private static System.Text.Encoding? GetEncodingFromHeader(string? encodingName)
    {
        if (string.IsNullOrEmpty(encodingName))
            return System.Text.Encoding.UTF8;
        
        try
        {
            return System.Text.Encoding.GetEncoding(encodingName);
        }
        catch
        {
            return System.Text.Encoding.UTF8;
        }
    }
}