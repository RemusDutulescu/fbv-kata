using static FunBooksAndVideos.Domain.Tests.PurchaseOrders;

namespace FunBooksAndVideos.Domain.Tests;

public sealed class PurchaseOrderTests
{
    [Fact]
    public void An_order_needs_at_least_one_line() =>
        Assert.Throws<DomainException>(() => AnOrder());

    [Fact]
    public void An_order_has_a_maximum_number_of_lines() =>
        Assert.Throws<DomainException>(() => AnOrder([.. Enumerable.Repeat<ItemLine>(new BookLine("Dune"), PurchaseOrder.MaxLines + 1)]));

    [Fact]
    public void An_order_keeps_its_own_copy_of_the_lines()
    {
        List<ItemLine> lines = [new BookLine("Dune")];
        var order = new PurchaseOrder(new PurchaseOrderId("1"), new CustomerId("2"), new Money(10m), lines);

        lines.Add(new VideoLine("Added later"));

        Assert.Single(order.Lines);
    }
}
