using FunBooksAndVideos.Infrastructure.Json;
using FunBooksAndVideos.Infrastructure.Observability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Temporalio.Converters;
using Temporalio.Extensions.DiagnosticSource;
using Temporalio.Extensions.Hosting;
using Temporalio.Extensions.OpenTelemetry;
using Temporalio.Runtime;

namespace FunBooksAndVideos.Infrastructure.Temporal;

public static class TemporalServiceCollectionExtensions
{
    /// <summary>Temporal payloads use the same JSON as the database, so ids and lines read naturally in the Temporal UI.</summary>
    public static DataConverter TemporalDataConverter { get; } =
        DataConverter.Default with { PayloadConverter = new DefaultPayloadConverter(DomainJson.Options) };

    // One runtime per process, as the SDK advises. It reports the SDK's metrics through Telemetry.TemporalMeter.
    private static readonly TemporalRuntime Runtime = new(new()
    {
        Telemetry = new()
        {
            Metrics = new()
            {
                CustomMetricMeter = new CustomMetricMeter(Telemetry.TemporalMeter),
                AttachServiceName = false, // otherwise every series says service_name="temporal-core-sdk"
            },
        },
    });

    /// <summary>The Temporal client, for both hosts: the API starts workflows with it, and the worker polls with it.</summary>
    public static TemporalOptions AddTemporal(this IServiceCollection services, IConfiguration configuration)
    {
        var temporal = services.AddRequiredOptions<TemporalOptions>(configuration, TemporalOptions.Section);

        services.AddTemporalClient(client =>
        {
            client.TargetHost = temporal.Address;
            client.Namespace = temporal.Namespace;
            client.DataConverter = TemporalDataConverter;
            client.Runtime = Runtime;

            // Carries the trace context in workflow and activity headers.
            client.Interceptors = [new TracingInterceptor()];
            if (!string.IsNullOrEmpty(temporal.ApiKey))
            {
                client.ApiKey = temporal.ApiKey;
                client.Tls = new();
            }
        });

        return temporal;
    }
}
