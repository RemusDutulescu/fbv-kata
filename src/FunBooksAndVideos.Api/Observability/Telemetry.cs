using System.Diagnostics;
using Azure.Monitor.OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Temporalio.Extensions.OpenTelemetry;

namespace FunBooksAndVideos.Api.Observability;

/// <summary>
/// One trace per order, from the HTTP request through the workflow and its activities down to each rule and SQL
/// statement. Temporal carries the trace context in its headers, so the trace survives retries and worker restarts.
/// </summary>
public static class Telemetry
{
    public const string ServiceName = "FunBooksAndVideos";

    /// <summary>Spans of this application's own making: one per rule evaluation.</summary>
    public static ActivitySource Source { get; } = new(ServiceName);

    public static IServiceCollection AddTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        // Every trace is kept; add a sampler when the traffic makes Application Insights ingestion cost.
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(ServiceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(Source.Name)
                    .AddSource(TracingInterceptor.ClientSource.Name, TracingInterceptor.WorkflowsSource.Name, TracingInterceptor.ActivitiesSource.Name)
                    // Probes call every few seconds; their traces would be noise, and they'd cost ingestion.
                    .AddAspNetCoreInstrumentation(http => http.Filter = context => !context.Request.Path.StartsWithSegments("/health"))
                    .AddSqlClientInstrumentation();

                // Azure exports to Application Insights; locally, docker compose runs an Aspire dashboard on OTLP.
                if (configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"] is { Length: > 0 } connectionString)
                    tracing.AddAzureMonitorTraceExporter(exporter => exporter.ConnectionString = connectionString);
                else if (configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] is { Length: > 0 })
                    tracing.AddOtlpExporter();
            });

        return services;
    }
}
