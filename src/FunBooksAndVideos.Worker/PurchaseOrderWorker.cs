using FunBooksAndVideos.Application.Processing;
using FunBooksAndVideos.Domain.Rules;
using FunBooksAndVideos.Infrastructure.Observability;
using FunBooksAndVideos.Infrastructure.Persistence;
using FunBooksAndVideos.Infrastructure.Temporal;
using Temporalio.Extensions.Hosting;

namespace FunBooksAndVideos.Worker;

/// <summary>
/// The worker host: it runs every order's workflow and activities, and nothing else. It deploys and scales apart from
/// the API, so more replicas mean more orders processed at once (ADR 8).
/// </summary>
public static class PurchaseOrderWorker
{
    public static IServiceCollection AddPurchaseOrderWorker(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddShopDatabase(configuration);
        var temporal = services.AddTemporal(configuration);
        services.AddTelemetry(configuration, "FunBooksAndVideos.Worker");

        // The business rules. A new rule is a new class and one line here.
        services.AddRule<MembershipActivationRule>();
        services.AddRule<ShippingSlipRule>();
        services.AddSingleton<PurchaseOrderProcessor>();

        services.AddHostedTemporalWorker(temporal.TaskQueue)
            .AddWorkflow<PurchaseOrderWorkflow>()
            .AddScopedActivities<OrderActivities>()
            .AddScopedActivities<FulfilmentActivities>();

        return services;
    }

    /// <summary>Registers a rule wrapped in a <see cref="TracedRule"/>, so every rule shows up in the trace.</summary>
    private static void AddRule<TRule>(this IServiceCollection services)
        where TRule : class, IPurchaseOrderRule =>
        services.AddSingleton<IPurchaseOrderRule>(provider => new TracedRule(ActivatorUtilities.CreateInstance<TRule>(provider)));
}
