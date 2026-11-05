namespace FunBooksAndVideos.Domain.Tests;

/// <summary>Object mother: the orders the tests talk about, by name.</summary>
public static class PurchaseOrders
{
    /// <summary>The purchase order from the brief, verbatim.</summary>
    public static PurchaseOrder BriefExample() =>
        APurchaseOrder()
            .WithId("3344656")
            .ForCustomer("4567890")
            .WithTotal(48.50m)
            .WithVideo("Comprehensive First Aid Training")
            .WithBook("The Girl on the train")
            .WithMembership(MembershipType.BookClub)
            .Build();

    public static PurchaseOrderBuilder APurchaseOrder() => new();
}

/// <summary>Test data builder: sensible defaults for everything a test doesn't care about.</summary>
public sealed class PurchaseOrderBuilder
{
    private readonly List<ItemLine> _lines = [];
    private string _id = "1000";
    private string _customerId = "2000";
    private decimal _total;

    public PurchaseOrderBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public PurchaseOrderBuilder ForCustomer(string customerId)
    {
        _customerId = customerId;
        return this;
    }

    public PurchaseOrderBuilder WithTotal(decimal total)
    {
        _total = total;
        return this;
    }

    public PurchaseOrderBuilder WithBook(string title = "A book")
    {
        _lines.Add(new BookLine(title));
        return this;
    }

    public PurchaseOrderBuilder WithVideo(string title = "A video")
    {
        _lines.Add(new VideoLine(title));
        return this;
    }

    public PurchaseOrderBuilder WithMembership(MembershipType membership)
    {
        _lines.Add(new MembershipLine(membership));
        return this;
    }

    public PurchaseOrder Build() => new(new PurchaseOrderId(_id), new CustomerId(_customerId), new Money(_total), _lines);
}
