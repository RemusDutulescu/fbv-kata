using FunBooksAndVideos.Application.Processing;
using FunBooksAndVideos.Domain;
using FunBooksAndVideos.Infrastructure.Persistence;
using FunBooksAndVideos.Infrastructure.Temporal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Temporalio.Client;
using Temporalio.Exceptions;

namespace FunBooksAndVideos.Application.Submission;

/// <summary>Hands an order to its workflow. The order id is the idempotency key (ADR 3).</summary>
public sealed class OrderSubmitter(
    ITemporalClient temporal, ShopDbContext db, IOptions<TemporalOptions> temporalOptions, IOptions<SubmissionOptions> options)
{
    public async Task<SubmissionOutcome> SubmitAsync(PurchaseOrder order, CancellationToken cancellationToken)
    {
        // A fulfilled order answers a retry straight from SQL. A retry of one still in progress goes on to join its
        // workflow, so it too hears back only once the memberships are active.
        var stored = await StoredAsync(order.Id, cancellationToken);
        if (stored is not null && !stored.IsSameOrderAs(order)) return SubmissionOutcome.Conflict;
        if (stored is { Status: PurchaseOrderStatus.Completed }) return SubmissionOutcome.Replayed;

        var outcome = stored is null ? SubmissionOutcome.Created : SubmissionOutcome.Replayed;
        try
        {
            // Update-with-Start: one call starts the workflow, or joins it, and waits for its Submit update, which
            // completes once the Immediate commands (BR1) have run. The update id comes from the order id, so for a
            // workflow that has already finished, Temporal answers with that update's original result.
            await temporal.ExecuteUpdateWithStartWorkflowAsync(
                (PurchaseOrderWorkflow workflow) => workflow.SubmitAsync(),
                new WorkflowUpdateWithStartOptions(
                    $"submit-{order.Id}",
                    WithStartWorkflowOperation.Create(
                        (PurchaseOrderWorkflow workflow) => workflow.RunAsync(OrderSubmission.From(order)),
                        PurchaseOrderWorkflow.StartOptions(order.Id, temporalOptions.Value.TaskQueue)))
                {
                    Rpc = new() { Timeout = options.Value.AcknowledgementTimeout, CancellationToken = cancellationToken },
                });
        }
        catch (Exception e) when (IsUnanswered(e) && !cancellationToken.IsCancellationRequested)
        {
            // Whatever Temporal received carries on regardless.
            outcome = SubmissionOutcome.Pending;
        }
        catch (WorkflowAlreadyStartedException)
        {
            // Its workflow ended without acknowledging (terminated by an operator, say), so its memberships may never
            // have been activated, and it won't run again (ADR 3).
            outcome = SubmissionOutcome.Conflict;
        }

        // Whatever Temporal said, the stored order says whose id this is.
        return (await StoredAsync(order.Id, cancellationToken), outcome) switch
        {
            ({ } first, _) when !first.IsSameOrderAs(order) => SubmissionOutcome.Conflict, // a different order got there first
            (null, SubmissionOutcome.Pending) => SubmissionOutcome.Unconfirmed,            // not even stored: nothing is known
            (null, _) => SubmissionOutcome.Conflict,                                       // its workflow ran, its order is gone
            _ => outcome,
        };
    }

    /// <summary>No answer in time, Temporal out of reach or throttling, or a lazy client that has never connected.</summary>
    private bool IsUnanswered(Exception e) =>
        e is RpcTimeoutOrCanceledException
            or RpcException { Code: RpcException.StatusCode.Unavailable or RpcException.StatusCode.ResourceExhausted }
        || (e is InvalidOperationException && !temporal.Connection.IsConnected);

    private Task<PurchaseOrderRecord?> StoredAsync(PurchaseOrderId id, CancellationToken cancellationToken) =>
        db.PurchaseOrders.AsNoTracking().SingleOrDefaultAsync(o => o.Id == id, cancellationToken);
}
