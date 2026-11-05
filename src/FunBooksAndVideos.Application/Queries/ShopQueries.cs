using FunBooksAndVideos.Domain;
using FunBooksAndVideos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FunBooksAndVideos.Application.Queries;

/// <summary>An order as stored, with its shipping slip once one is generated.</summary>
public sealed record StoredOrder(PurchaseOrderRecord Order, ShippingSlipRecord? Slip);

/// <summary>The read side: orders and memberships come straight from SQL, never through Temporal.</summary>
public sealed class ShopQueries(ShopDbContext db)
{
    public async Task<StoredOrder?> FindOrderAsync(PurchaseOrderId id, CancellationToken cancellationToken)
    {
        // One round trip for the order and, once generated, its shipping slip.
        var found = await db.PurchaseOrders.AsNoTracking()
            .Where(order => order.Id == id)
            .LeftJoin(db.ShippingSlips, order => order.Id, slip => slip.PurchaseOrderId, (order, slip) => new { order, slip })
            .SingleOrDefaultAsync(cancellationToken);

        return found is null ? null : new StoredOrder(found.order, found.slip);
    }

    /// <summary>The clubs a customer belongs to. A customer this service has never seen belongs to none.</summary>
    public Task<MembershipType> MembershipsOfAsync(CustomerId id, CancellationToken cancellationToken) =>
        db.Customers.AsNoTracking()
            .Where(customer => customer.Id == id)
            .Select(customer => customer.Memberships)
            .SingleOrDefaultAsync(cancellationToken);
}
