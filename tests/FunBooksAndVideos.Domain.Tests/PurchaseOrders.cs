namespace FunBooksAndVideos.Domain.Tests;

/// <summary>Object mother: the orders the tests talk about.</summary>
public static class PurchaseOrders
{
    /// <summary>The purchase order from the brief, verbatim.</summary>
    public static PurchaseOrder BriefExample() => new(
        new PurchaseOrderId("3344656"),
        new CustomerId("4567890"),
        new Money(48.50m),
        [
            new VideoLine("Comprehensive First Aid Training"),
            new BookLine("The Girl on the train"),
            new MembershipLine(MembershipType.BookClub),
        ]);

    /// <summary>An order of these lines; its id, customer and total are defaults the test doesn't care about.</summary>
    public static PurchaseOrder AnOrder(params ItemLine[] lines) =>
        new(new PurchaseOrderId("1000"), new CustomerId("2000"), new Money(0m), lines);
}
