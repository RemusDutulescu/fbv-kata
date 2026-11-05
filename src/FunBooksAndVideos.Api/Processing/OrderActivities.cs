using FunBooksAndVideos.Api.Persistence;
using FunBooksAndVideos.Domain;
using FunBooksAndVideos.Domain.Fulfilment;
using FunBooksAndVideos.Domain.Rules;
using Microsoft.EntityFrameworkCore;
using Temporalio.Activities;

namespace FunBooksAndVideos.Api.Processing;

/// <summary>The order's own lifecycle steps. Each activity gets a fresh scope, so one DbContext is one unit of work.</summary>
public sealed class OrderActivities(ShopDbContext db, PurchaseOrderProcessor processor)
{
    /// <summary>Stores the order and decides what has to happen to it.</summary>
    [Activity]
    public async Task<IReadOnlyList<FulfilmentCommand>> AcceptOrderAsync(OrderSubmission submission)
    {
        var cancellationToken = ActivityExecutionContext.Current.CancellationToken;
        var order = submission.ToPurchaseOrder();

        // Idempotent: a retry after the insert committed finds the order and moves on.
        if (!await db.PurchaseOrders.AnyAsync(o => o.Id == order.Id, cancellationToken))
        {
            db.PurchaseOrders.Add(PurchaseOrderRecord.Accept(order, DateTimeOffset.UtcNow));
            await db.SaveChangesAsync(cancellationToken);
        }

        return processor.Process(order);
    }

    [Activity]
    public Task CompleteOrderAsync(PurchaseOrderId id) =>
        db.PurchaseOrders
            .Where(o => o.Id == id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(o => o.Status, PurchaseOrderStatus.Completed),
                ActivityExecutionContext.Current.CancellationToken);
}
