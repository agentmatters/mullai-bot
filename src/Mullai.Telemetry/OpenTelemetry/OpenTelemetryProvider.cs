using Microsoft.Extensions.Configuration;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Mullai.OpenTelemetry.OpenTelemetry;

public static class OpenTelemetryProvider
{
    public static TracerProvider SetupTracerProvider(IConfiguration configuration)
    {
        return Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(
                OpenTelemetrySettings.ServiceName, serviceVersion: OpenTelemetrySettings.ServiceVersion))
            .AddSource(OpenTelemetrySettings.ServiceName) // Our custom activity source
            .AddSource("*Microsoft.Agents.AI") // Agent Framework telemetry
            .AddHttpClientInstrumentation() // Capture HTTP calls to OpenAI
            .AddOtlpExporter(options => options.Endpoint = new Uri(OpenTelemetrySettings.OtlpEndpoint))
            .Build();
    }

    public static MeterProvider SetupMeterProvider(IConfiguration configuration)
    {
        return Sdk.CreateMeterProviderBuilder()
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(
                OpenTelemetrySettings.ServiceName, serviceVersion: OpenTelemetrySettings.ServiceVersion))
            .AddMeter(OpenTelemetrySettings.ServiceName) // Our custom meter
            .AddMeter("*Microsoft.Agents.AI") // Agent Framework metrics
            .AddHttpClientInstrumentation() // HTTP client metrics
            .AddRuntimeInstrumentation() // .NET runtime metrics
            .AddOtlpExporter(options => options.Endpoint = new Uri(OpenTelemetrySettings.OtlpEndpoint))
            .Build();
    }
}