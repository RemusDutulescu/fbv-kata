using FunBooksAndVideos.Api.Persistence;
using FunBooksAndVideos.Api.Processing;
using FunBooksAndVideos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FunBooksAndVideos.Api.Tests;

/// <summary>The real thing end to end, minus HTTP: submitter, Temporal, worker, activities and SQL Server.</summary>
public sealed class OrderProcessingTests(AppFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task The_brief_example_is_acknowledged_with_the_membership_already_active()
    {
        var order = new PurchaseOrder(
            new PurchaseOrderId("3344656"),
            new CustomerId("4567890"),
            new Money(48.50m),
            [
                new VideoLine("Comprehensive First Aid Training"),
                new BookLine("The Girl on the train"),
                new MembershipLine(MembershipType.BookClub),
            ]);

        var outcome = await SubmitAsync(order);

        Assert.IsType<SubmissionOutcome.Created>(outcome);
        // BR1: active the moment the caller hears back, not eventually.
        Assert.Equal(MembershipType.BookClub, await MembershipsAsync(order.CustomerId));

        await WorkflowFinishedAsync(order.Id);
        await using var db = fixture.CreateDbContext();
        var slip = await db.ShippingSlips.SingleAsync(s => s.PurchaseOrderId == order.Id, Ct);
        // BR2: the book ships; the video is watched online.
        Assert.Equal(new BookLine("The Girl on the train"), Assert.Single(slip.Items));
        Assert.Equal(PurchaseOrderStatus.Completed, (await db.PurchaseOrders.SingleAsync(o => o.Id == order.Id, Ct)).Status);
    }

    [Fact]
    public async Task Resubmitting_the_same_order_is_a_replay()
    {
        var order = AnOrder(NewId(), new CustomerId(NewId()), MembershipType.VideoClub);

        Assert.IsType<SubmissionOutcome.Created>(await SubmitAsync(order));
        Assert.IsType<SubmissionOutcome.Replayed>(await SubmitAsync(order));

        await WorkflowFinishedAsync(order.Id);
        await using var db = fixture.CreateDbContext();
        Assert.Equal(1, await db.ShippingSlips.CountAsync(s => s.PurchaseOrderId == order.Id, Ct));
    }

    [Fact]
    public async Task Reusing_an_id_for_a_different_order_is_a_conflict()
    {
        var id = NewId();
        var customer = new CustomerId(NewId());

        Assert.IsType<SubmissionOutcome.Created>(await SubmitAsync(AnOrder(id, customer, MembershipType.BookClub)));
        Assert.IsType<SubmissionOutcome.Conflict>(await SubmitAsync(AnOrder(id, customer, MembershipType.VideoClub)));
    }

    [Fact]
    public async Task Two_simultaneous_orders_for_one_customer_end_in_premium()
    {
        var customer = new CustomerId(NewId());

        // Both orders race to create and update the same customer row; the loser retries and builds on the winner.
        var outcomes = await Task.WhenAll(
            SubmitAsync(AnOrder(NewId(), customer, MembershipType.BookClub)),
            SubmitAsync(AnOrder(NewId(), customer, MembershipType.VideoClub)));

        Assert.All(outcomes, outcome => Assert.IsType<SubmissionOutcome.Created>(outcome));
        Assert.Equal(MembershipType.Premium, await MembershipsAsync(customer));
    }

    private static PurchaseOrder AnOrder(string id, CustomerId customer, MembershipType membership) =>
        new(new PurchaseOrderId(id), customer, new Money(25m), [new BookLine("Dune"), new MembershipLine(membership)]);

    private static string NewId() => Guid.NewGuid().ToString("N");

    private async Task<SubmissionOutcome> SubmitAsync(PurchaseOrder order)
    {
        await using var scope = fixture.App.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<OrderSubmitter>().SubmitAsync(order, Ct);
    }

    private Task WorkflowFinishedAsync(PurchaseOrderId id) =>
        fixture.Temporal.GetWorkflowHandle(PurchaseOrderWorkflow.IdFor(id)).GetResultAsync();

    private async Task<MembershipType> MembershipsAsync(CustomerId id)
    {
        await using var db = fixture.CreateDbContext();
        return (await db.Customers.SingleAsync(c => c.Id == id, Ct)).Memberships;
    }
}
