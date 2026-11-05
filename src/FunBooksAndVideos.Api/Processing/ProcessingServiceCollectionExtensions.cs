using FunBooksAndVideos.Domain.Rules;
using Microsoft.Extensions.Options;
using Temporalio.Client;
using Temporalio.Converters;
using Temporalio.Extensions.Hosting;

namespace FunBooksAndVideos.Api.Processing;

public static class ProcessingServiceCollectionExtensions
{
    /// <summary>Temporal payloads use the same JSON as the database, so ids and lines read naturally in the Temporal UI.</summary>
    public static DataConverter TemporalDataConverter { get; } =
        DataConverter.Default with { PayloadConverter = new DefaultPayloadConverter(DomainJson.Options) };

    public static IServiceCollection AddOrderProcessing(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(TemporalOptions.Section);
        services.AddOptions<TemporalOptions>().Bind(section).ValidateDataAnnotations().ValidateOnStart();

        // The business rules. A new rule is one class plus one line here; the processor never changes.
        services.AddSingleton<IPurchaseOrderRule, MembershipActivationRule>();
        services.AddSingleton<IPurchaseOrderRule, ShippingSlipRule>();
        services.AddSingleton<PurchaseOrderProcessor>();

        services.AddTemporalClient(_ => { });
        services.AddOptions<TemporalClientConnectOptions>().Configure<IOptions<TemporalOptions>>((client, temporal) =>
        {
            client.TargetHost = temporal.Value.Address;
            client.Namespace = temporal.Value.Namespace;
            client.DataConverter = TemporalDataConverter;
            if (!string.IsNullOrEmpty(temporal.Value.ApiKey))
            {
                client.ApiKey = temporal.Value.ApiKey;
                client.Tls = new();
            }
        });

        // The worker runs inside the API process: one deployable. Split it out when workers need to scale on their own.
        var taskQueue = section[nameof(TemporalOptions.TaskQueue)] ?? new TemporalOptions().TaskQueue;
        services.AddHostedTemporalWorker(taskQueue)
            .AddScopedActivities<OrderActivities>()
            .AddScopedActivities<FulfilmentActivities>()
            .AddWorkflow<PurchaseOrderWorkflow>();

        services.AddScoped<OrderSubmitter>();
        return services;
    }
}
