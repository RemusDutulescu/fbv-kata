using FunBooksAndVideos.Domain;

namespace FunBooksAndVideos.Infrastructure.Persistence;

/// <summary>Where an order's fulfilment stands, as projected by the workflow for readers.</summary>
public enum PurchaseOrderStatus
{
    /// <summary>Accepted; fulfilment is in progress.</summary>
    Processing,

    /// <summary>Every fulfilment command has run.</summary>
    Completed,
}

/// <summary>An accepted order as stored: the order plus where its fulfilment stands. The workflow writes it; the API reads it.</summary>
public sealed class PurchaseOrderRecord
{
    public required PurchaseOrderId Id { get; init; }

    public required CustomerId CustomerId { get; init; }

    public required Money Total { get; init; }

    public required IReadOnlyList<ItemLine> Lines { get; init; }

    public PurchaseOrderStatus Status { get; set; }

    public DateTimeOffset AcceptedAt { get; init; }

    public static PurchaseOrderRecord Accept(PurchaseOrder order, DateTimeOffset acceptedAt) => new()
    {
        Id = order.Id,
        CustomerId = order.CustomerId,
        Total = order.Total,
        Lines = order.Lines,
        Status = PurchaseOrderStatus.Processing,
        AcceptedAt = acceptedAt,
    };

    /// <summary>A submission of this same order is a retry; anything else under its id is a different order. Compared by value.</summary>
    public bool IsSameOrderAs(PurchaseOrder order) =>
        CustomerId == order.CustomerId && Total == order.Total && Lines.SequenceEqual(order.Lines);
}

/// <summary>A generated shipping slip (BR2): one per order, keyed by the order id.</summary>
public sealed class ShippingSlipRecord
{
    public required PurchaseOrderId PurchaseOrderId { get; init; }

    public required CustomerId CustomerId { get; init; }

    public required IReadOnlyList<ProductLine> Items { get; init; }

    public DateTimeOffset GeneratedAt { get; init; }
}
