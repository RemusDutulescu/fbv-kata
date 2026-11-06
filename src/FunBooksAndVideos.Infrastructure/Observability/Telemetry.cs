using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Temporalio.Extensions.OpenTelemetry;

namespace FunBooksAndVideos.Infrastructure.Observability;

/// <summary>
/// One trace per order, from the HTTP request through the workflow and its activities down to each rule and SQL
/// statement. Temporal carries the trace context in its headers, so the trace survives retries and worker restarts,
/// and crosses from the API's process to a worker's. Metrics and logs go the same way as the traces.
/// </summary>
public static class Telemetry
{
    /// <summary>Spans of this application's own making: one per rule evaluation.</summary>
    public static ActivitySource Source { get; } = new("FunBooksAndVideos");

    /// <summary>The Temporal SDK's own metrics: worker task slots, schedule-to-start latency, request latency.</summary>
    public static Meter TemporalMeter { get; } = new("Temporalio");

    /// <summary>Telemetry for one host, under its own service name. The host adds its own instrumentation to the builder.</summary>
    public static OpenTelemetryBuilder AddTelemetry(this IServiceCollection services, IConfiguration configuration, string serviceName)
    {
        var telemetry = services.AddOpenTelemetry()
            // The hostname tells replicas apart, so three workers show as three.
            .ConfigureResource(resource => resource.AddService(serviceName, serviceInstanceId: Environment.MachineName))
            .WithTracing(tracing => tracing
                .AddSource(Source.Name)
                .AddSource(TracingInterceptor.ClientSource.Name, TracingInterceptor.WorkflowsSource.Name, TracingInterceptor.ActivitiesSource.Name)
                .AddSqlClientInstrumentation())
            // .NET's own runtime meter: the dashboard's CPU and memory per replica.
            .WithMetrics(metrics => metrics.AddMeter(TemporalMeter.Name, "System.Runtime"))
            .WithLogging(configureBuilder: null, configureOptions: logging => logging.IncludeFormattedMessage = true);

        // To Grafana's LGTM stack locally, and to the host's collector on a host; with no endpoint, nothing is exported.
        if (configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] is { Length: > 0 })
            telemetry.UseOtlpExporter();

        return telemetry;
    }
}
