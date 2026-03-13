using Microsoft.Extensions.Configuration;

namespace Mullai.OpenTelemetry.OpenTelemetry;

public class OpenTelemetrySettings
{
    public static string OtlpEndpoint { get; private set; } = string.Empty;
    public static string ServiceName { get; private set; } = string.Empty;
    public static string ServiceVersion { get; private set; } = string.Empty;

    public static void Initialize(IConfiguration configuration)
    {
        var otlpEndpoint = configuration["OpenTelemetry:OTEL_EXPORTER_OTLP_ENDPOINT"];
        var serviceName = configuration["OpenTelemetry:ServiceName"];
        var serviceVersion = configuration["OpenTelemetry:ServiceVersion"];

        // Validation logic
        if (string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            throw new InvalidOperationException("OpenTelemetry:OTEL_EXPORTER_OTLP_ENDPOINT is missing from configuration.");
        }

        if (string.IsNullOrWhiteSpace(serviceName))
        {
            throw new InvalidOperationException("OpenTelemetry:ServiceName is missing from configuration.");
        }

        if (string.IsNullOrWhiteSpace(serviceVersion))
        {
            throw new InvalidOperationException("OpenTelemetry:ServiceVersion is missing from configuration.");
        }

        // Assign values after validation passes
        OtlpEndpoint = otlpEndpoint;
        ServiceName = serviceName;
        ServiceVersion = serviceVersion;
    }
}