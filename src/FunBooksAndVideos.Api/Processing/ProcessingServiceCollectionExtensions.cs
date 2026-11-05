using FunBooksAndVideos.Api.Observability;
using FunBooksAndVideos.Domain.Rules;
using Temporalio.Converters;
using Temporalio.Extensions.Hosting;
using Temporalio.Extensions.OpenTelemetry;

namespace FunBooksAndVideos.Api.Processing;

public static class ProcessingServiceCollectionExtensions
{
    /// <summary>Temporal payloads use the same JSON as the database, so ids and lines read naturally in the Temporal UI.</summary>
    public static DataConverter TemporalDataConverter { get; } =
        DataConverter.Default with { PayloadConverter = new DefaultPayloadConverter(DomainJson.Options) };

    public static IServiceCollection AddOrderProcessing(this IServiceCollection services, IConfiguration configuration)
    {
        var temporal = services.AddRequiredOptions<TemporalOptions>(configuration, TemporalOptions.Section);

        // The business rules.
        services.AddRule<MembershipActivationRule>();
        services.AddRule<ShippingSlipRule>();
        services.AddSingleton<PurchaseOrderProcessor>();

        services.AddTemporalClient(client =>
        {
            client.TargetHost = temporal.Address;
            client.Namespace = temporal.Namespace;
            client.DataConverter = TemporalDataConverter;

            // Carries the trace context through workflow and activity headers; the hosted worker picks it up too.
            client.Interceptors = [new TracingInterceptor()];
            if (!string.IsNullOrEmpty(temporal.ApiKey))
            {
                client.ApiKey = temporal.ApiKey;
                client.Tls = new();
            }
        });

        // The worker runs inside the API process: one deployable. Split it out when workers need to scale on their own.
        services.AddHostedTemporalWorker(temporal.TaskQueue)
            .AddScopedActivities<OrderActivities>()
            .AddScopedActivities<FulfilmentActivities>()
            .AddWorkflow<PurchaseOrderWorkflow>();

        services.AddScoped<OrderSubmitter>();
        return services;
    }

    /// <summary>Registers a rule wrapped in a <see cref="TracedRule"/>, so every rule shows up in the trace.</summary>
    private static void AddRule<TRule>(this IServiceCollection services)
        where TRule : class, IPurchaseOrderRule =>
        services.AddSingleton<IPurchaseOrderRule>(provider => new TracedRule(ActivatorUtilities.CreateInstance<TRule>(provider)));
}
