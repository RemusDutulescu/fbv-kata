using FunBooksAndVideos.Infrastructure.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Temporalio.Converters;
using Temporalio.Extensions.Hosting;
using Temporalio.Extensions.OpenTelemetry;

namespace FunBooksAndVideos.Infrastructure.Temporal;

public static class TemporalServiceCollectionExtensions
{
    /// <summary>Temporal payloads use the same JSON as the database, so ids and lines read naturally in the Temporal UI.</summary>
    public static DataConverter TemporalDataConverter { get; } =
        DataConverter.Default with { PayloadConverter = new DefaultPayloadConverter(DomainJson.Options) };

    /// <summary>The Temporal client, for both hosts: the API starts workflows with it, and the worker polls with it.</summary>
    public static TemporalOptions AddTemporal(this IServiceCollection services, IConfiguration configuration)
    {
        var temporal = services.AddRequiredOptions<TemporalOptions>(configuration, TemporalOptions.Section);

        services.AddTemporalClient(client =>
        {
            client.TargetHost = temporal.Address;
            client.Namespace = temporal.Namespace;
            client.DataConverter = TemporalDataConverter;

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
