using FunBooksAndVideos.Domain.Fulfilment;
using Temporalio.Workflows;

namespace FunBooksAndVideos.Application.Processing;

/// <summary>
/// Proxy: the receiver the workflow hands its commands to. It has the same interface as the real
/// <see cref="FulfilmentActivities"/>, but every call is scheduled as a durable, retried activity
/// instead of running inside workflow code, which has to stay deterministic.
/// </summary>
internal sealed class ActivityFulfilment : IFulfilment
{
    public Task ActivateMembershipAsync(ActivateMembership command) =>
        Workflow.ExecuteActivityAsync(
            (FulfilmentActivities activities) => activities.ActivateMembershipAsync(command),
            ActivityDefaults.Options);

    public Task GenerateShippingSlipAsync(GenerateShippingSlip command) =>
        Workflow.ExecuteActivityAsync(
            (FulfilmentActivities activities) => activities.GenerateShippingSlipAsync(command),
            ActivityDefaults.Options);
}
