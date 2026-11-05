using FunBooksAndVideos.Api.Persistence;
using FunBooksAndVideos.Domain;
using FunBooksAndVideos.Domain.Fulfilment;
using Microsoft.EntityFrameworkCore;
using Temporalio.Activities;

namespace FunBooksAndVideos.Api.Processing;

/// <summary>
/// The real receiver of fulfilment commands. Every method is a Temporal activity and one unit of work,
/// and every one is safe to run twice, because Temporal guarantees at-least-once execution.
/// </summary>
public sealed class FulfilmentActivities(ShopDbContext db) : IFulfilment
{
    [Activity]
    public async Task ActivateMembershipAsync(ActivateMembership command)
    {
        var cancellationToken = ActivityExecutionContext.Current.CancellationToken;

        var customer = await db.Customers.FindAsync([command.CustomerId], cancellationToken);
        if (customer is null)
        {
            customer = new Customer(command.CustomerId);
            db.Customers.Add(customer);
        }

        customer.Activate(command.Membership);

        // If another order changes this customer at the same moment, this throws (row version, or the primary key
        // for a brand-new customer); Temporal retries the activity, which then builds on the other order's change.
        await db.SaveChangesAsync(cancellationToken);
    }

    [Activity]
    public async Task GenerateShippingSlipAsync(GenerateShippingSlip command)
    {
        var cancellationToken = ActivityExecutionContext.Current.CancellationToken;

        // One slip per order: a retry after the insert committed has nothing left to do.
        if (await db.ShippingSlips.AnyAsync(s => s.PurchaseOrderId == command.PurchaseOrderId, cancellationToken))
            return;

        db.ShippingSlips.Add(new ShippingSlipRecord
        {
            PurchaseOrderId = command.PurchaseOrderId,
            CustomerId = command.CustomerId,
            Items = command.Items,
            GeneratedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
