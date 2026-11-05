namespace FunBooksAndVideos.Domain.Tests;

public sealed class PurchaseOrderTests
{
    [Fact]
    public void The_brief_example_is_a_valid_order()
    {
        var order = PurchaseOrders.BriefExample();

        Assert.Equal(new PurchaseOrderId("3344656"), order.Id);
        Assert.Equal(new CustomerId("4567890"), order.CustomerId);
        Assert.Equal(new Money(48.50m), order.Total);
        Assert.Collection(
            order.Lines,
            line => Assert.Equal(new VideoLine("Comprehensive First Aid Training"), line),
            line => Assert.Equal(new BookLine("The Girl on the train"), line),
            line => Assert.Equal(new MembershipLine(MembershipType.BookClub), line));
    }

    [Fact]
    public void An_order_needs_at_least_one_line() =>
        Assert.Throws<DomainException>(() => PurchaseOrders.APurchaseOrder().Build());

    [Fact]
    public void An_order_has_a_maximum_number_of_lines()
    {
        var builder = PurchaseOrders.APurchaseOrder();
        for (var i = 0; i <= PurchaseOrder.MaxLines; i++) builder.WithBook();

        Assert.Throws<DomainException>(() => builder.Build());
    }

    [Fact]
    public void An_order_keeps_its_own_copy_of_the_lines()
    {
        List<ItemLine> lines = [new BookLine("A book")];
        var order = new PurchaseOrder(new PurchaseOrderId("1"), new CustomerId("2"), new Money(10m), lines);

        lines.Add(new VideoLine("Added later"));

        Assert.Single(order.Lines);
    }
}
