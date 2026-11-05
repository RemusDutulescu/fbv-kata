using FunBooksAndVideos.Api.Persistence;
using FunBooksAndVideos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Temporalio.Api.Enums.V1;
using Temporalio.Client;
using Temporalio.Exceptions;

namespace FunBooksAndVideos.Api.Processing;

/// <summary>
/// Hands an order to its workflow. The order id is the idempotency key at three levels: a stored order answers
/// a retry straight from SQL, the workflow id stops a second workflow from starting, and the primary key backs both.
/// </summary>
public sealed class OrderSubmitter(ITemporalClient temporal, ShopDbContext db, IOptions<TemporalOptions> options)
{
    public async Task<SubmissionOutcome> SubmitAsync(PurchaseOrder order, CancellationToken cancellationToken)
    {
        var submission = OrderSubmission.From(order);

        var storedHash = await StoredHashAsync(order.Id, cancellationToken);
        if (storedHash is not null)
        {
            return storedHash == submission.PayloadHash ? SubmissionOutcome.Replayed : SubmissionOutcome.Conflict;
        }

        var start = WithStartWorkflowOperation.Create(
            (PurchaseOrderWorkflow workflow) => workflow.RunAsync(submission),
            new WorkflowOptions(PurchaseOrderWorkflow.IdFor(order.Id), options.Value.TaskQueue)
            {
                // A concurrent duplicate joins the running workflow; a finished one is never started again.
                IdConflictPolicy = WorkflowIdConflictPolicy.UseExisting,
                IdReusePolicy = WorkflowIdReusePolicy.RejectDuplicate,
            });

        try
        {
            // Update-with-Start: one call starts the workflow and waits for its Submit update, which completes once
            // the Immediate commands (BR1) have run. The update id gives a duplicate call the same answer.
            await temporal.ExecuteUpdateWithStartWorkflowAsync(
                (PurchaseOrderWorkflow workflow) => workflow.SubmitAsync(),
                new WorkflowUpdateWithStartOptions($"submit-{order.Id}", start)
                {
                    Rpc = new() { Timeout = options.Value.AcknowledgementTimeout, CancellationToken = cancellationToken },
                });
        }
        catch (RpcTimeoutOrCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Slower than promised. The workflow carries on regardless, so tell the caller where to look.
            return SubmissionOutcome.Pending;
        }
        catch (WorkflowAlreadyStartedException)
        {
            // A finished workflow already used this id but its order isn't stored (a wiped database): the id is spent.
            return SubmissionOutcome.Conflict;
        }

        storedHash = await StoredHashAsync(order.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Order {order.Id} was acknowledged but is not stored.");

        // A concurrent submission with the same id but different content may have started the workflow first.
        return storedHash == submission.PayloadHash ? SubmissionOutcome.Created : SubmissionOutcome.Conflict;
    }

    private Task<string?> StoredHashAsync(PurchaseOrderId id, CancellationToken cancellationToken) =>
        db.PurchaseOrders
            .Where(o => o.Id == id)
            .Select(o => o.PayloadHash)
            .FirstOrDefaultAsync(cancellationToken);
}
