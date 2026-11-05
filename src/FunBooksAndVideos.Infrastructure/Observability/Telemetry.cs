using System.Diagnostics;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Temporalio.Extensions.OpenTelemetry;

namespace FunBooksAndVideos.Infrastructure.Observability;

/// <summary>
/// One trace per order, from the HTTP request through the workflow and its activities down to each rule and SQL
/// statement. Temporal carries the trace context in its headers, so the trace survives retries and worker restarts,
/// and crosses from the API's process to a worker's.
/// </summary>
public static class Telemetry
{
    /// <summary>Spans of this application's own making: one per rule evaluation.</summary>
    public static ActivitySource Source { get; } = new("FunBooksAndVideos");

    /// <summary>Telemetry for one host, under its own service name. The host adds its own instrumentation to the builder.</summary>
    public static OpenTelemetryBuilder AddTelemetry(this IServiceCollection services, IConfiguration configuration, string serviceName) =>
        // Every trace is kept; add a sampler when the traffic makes Application Insights ingestion cost.
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(Source.Name)
                    .AddSource(TracingInterceptor.ClientSource.Name, TracingInterceptor.WorkflowsSource.Name, TracingInterceptor.ActivitiesSource.Name)
                    .AddSqlClientInstrumentation();

                // Azure exports to Application Insights; locally, docker compose runs an Aspire dashboard on OTLP.
                if (configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"] is { Length: > 0 } connectionString)
                    tracing.AddAzureMonitorTraceExporter(exporter => exporter.ConnectionString = connectionString);
                else if (configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] is { Length: > 0 })
                    tracing.AddOtlpExporter();
            });
}
