using FunBooksAndVideos.Domain;
using FunBooksAndVideos.Domain.Fulfilment;
using Temporalio.Api.Enums.V1;
using Temporalio.Client;
using Temporalio.Common;
using Temporalio.Workflows;

namespace FunBooksAndVideos.Api.Processing;

/// <summary>
/// Process manager for one purchase order, and the invoker of its fulfilment commands. Its id comes from the order id
/// (ADR 3), and its recovery is forward-only: every step is idempotent and retried until it succeeds (ADR 4).
/// </summary>
[Workflow]
public sealed class PurchaseOrderWorkflow
{
    private bool _acknowledged;

    public static string IdFor(PurchaseOrderId id) => $"purchase-order-{id.Value}";

    /// <summary>A concurrent duplicate joins the running workflow; a finished one is never started again.</summary>
    public static WorkflowOptions StartOptions(PurchaseOrderId id, string taskQueue) => new(IdFor(id), taskQueue)
    {
        IdConflictPolicy = WorkflowIdConflictPolicy.UseExisting,
        IdReusePolicy = WorkflowIdReusePolicy.RejectDuplicate,
    };

    [WorkflowRun]
    public async Task RunAsync(OrderSubmission submission)
    {
        // The rules run inside an activity, not in this method: their decision is recorded in the workflow history,
        // so deploying a changed rule can never break the replay of orders already in flight.
        var commands = await Workflow.ExecuteActivityAsync(
            (OrderActivities activities) => activities.AcceptOrderAsync(submission),
            ActivityDefaults.Options);

        var receiver = new ActivityFulfilment();

        foreach (var command in commands.Where(c => c.Timing == Timing.Immediate))
            await command.ExecuteAsync(receiver);

        // The Submit update completes now, so the caller hears back only once BR1's activation is done.
        _acknowledged = true;

        foreach (var command in commands.Where(c => c.Timing == Timing.Deferred))
            await command.ExecuteAsync(receiver);

        await Workflow.ExecuteActivityAsync(
            (OrderActivities activities) => activities.CompleteOrderAsync(submission.Id),
            ActivityDefaults.Options);
    }

    /// <summary>Sent in the same call as the start (Update-with-Start). Completes once the Immediate commands have run.</summary>
    [WorkflowUpdate]
    public Task SubmitAsync() => Workflow.WaitConditionAsync(() => _acknowledged);
}

internal static class ActivityDefaults
{
    // Unlimited retries suit transient faults; a step that keeps failing stays visible in the Temporal UI
    // for an operator instead of failing a paid order. Add MaximumAttempts plus an alert if that ever needs a ceiling.
    public static readonly ActivityOptions Options = new()
    {
        StartToCloseTimeout = TimeSpan.FromSeconds(30),
        RetryPolicy = new RetryPolicy
        {
            InitialInterval = TimeSpan.FromSeconds(1),
            BackoffCoefficient = 2,
            MaximumInterval = TimeSpan.FromMinutes(1),
            NonRetryableErrorTypes = [nameof(DomainException)],
        },
    };
}
