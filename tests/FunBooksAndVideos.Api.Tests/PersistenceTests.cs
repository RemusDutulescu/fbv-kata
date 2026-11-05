using FunBooksAndVideos.Api.Persistence;
using FunBooksAndVideos.Domain;
using Microsoft.EntityFrameworkCore;

namespace FunBooksAndVideos.Api.Tests;

public sealed class PersistenceTests(AppFixture sql)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_customer_round_trips_with_its_memberships()
    {
        var id = new CustomerId(NewId());
        await using (var db = sql.CreateDbContext())
        {
            var customer = new Customer(id);
            customer.Activate(MembershipType.BookClub);
            db.Customers.Add(customer);
            await db.SaveChangesAsync(Ct);
        }

        await using var read = sql.CreateDbContext();
        var stored = await read.Customers.SingleAsync(c => c.Id == id, Ct);

        Assert.Equal(MembershipType.BookClub, stored.Memberships);
    }

    [Fact]
    public async Task A_concurrent_change_to_the_same_customer_is_detected()
    {
        var id = new CustomerId(NewId());
        await using (var db = sql.CreateDbContext())
        {
            db.Customers.Add(new Customer(id));
            await db.SaveChangesAsync(Ct);
        }

        await using var first = sql.CreateDbContext();
        await using var second = sql.CreateDbContext();
        var seenByFirst = await first.Customers.SingleAsync(c => c.Id == id, Ct);
        var seenBySecond = await second.Customers.SingleAsync(c => c.Id == id, Ct);

        seenByFirst.Activate(MembershipType.BookClub);
        seenBySecond.Activate(MembershipType.VideoClub);
        await first.SaveChangesAsync(Ct);

        // Without the row version this save would silently overwrite BookClub with VideoClub.
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync(Ct));
    }

    [Fact]
    public async Task An_order_keeps_its_polymorphic_lines_and_total()
    {
        var id = new PurchaseOrderId(NewId());
        ItemLine[] lines =
        [
            new VideoLine("Comprehensive First Aid Training"),
            new BookLine("The Girl on the train"),
            new MembershipLine(MembershipType.BookClub),
        ];
        var order = new PurchaseOrder(id, new CustomerId("4567890"), new Money(48.50m), lines);

        await using (var db = sql.CreateDbContext())
        {
            db.PurchaseOrders.Add(PurchaseOrderRecord.Accept(order, DateTimeOffset.UtcNow));
            await db.SaveChangesAsync(Ct);
        }

        await using var read = sql.CreateDbContext();
        var stored = await read.PurchaseOrders.SingleAsync(o => o.Id == id, Ct);

        Assert.Equal(lines, stored.Lines);
        Assert.Equal(new Money(48.50m), stored.Total);
        Assert.Equal(PurchaseOrderStatus.Processing, stored.Status);
    }

    [Fact]
    public async Task Ids_are_case_sensitive_as_in_temporal()
    {
        var id = NewId();
        await using (var db = sql.CreateDbContext())
        {
            db.Customers.Add(new Customer(new CustomerId(id)));
            await db.SaveChangesAsync(Ct);
        }

        await using var read = sql.CreateDbContext();
        var upperCase = new CustomerId(id.ToUpperInvariant());

        Assert.False(await read.Customers.AnyAsync(c => c.Id == upperCase, Ct));
    }

    private static string NewId() => Guid.NewGuid().ToString("N");
}
