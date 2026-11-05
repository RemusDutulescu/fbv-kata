using FunBooksAndVideos.Domain;

namespace FunBooksAndVideos.Api.Persistence;

/// <summary>Where an order's fulfilment stands, as projected by the workflow for readers.</summary>
public enum PurchaseOrderStatus
{
    /// <summary>Accepted; fulfilment is in progress.</summary>
    Processing,

    /// <summary>Every fulfilment command has run.</summary>
    Completed,
}

/// <summary>
/// An accepted order as stored: the order plus where its fulfilment stands. The workflow writes it and the
/// API reads it. A domain <see cref="PurchaseOrder"/> never changes once accepted, so a record is all it needs.
/// </summary>
public sealed class PurchaseOrderRecord
{
    public required PurchaseOrderId Id { get; init; }

    public required CustomerId CustomerId { get; init; }

    public required Money Total { get; init; }

    public required IReadOnlyList<ItemLine> Lines { get; init; }

    /// <summary>SHA-256 of the submitted order: the same hash means a retry, a different one means a conflicting reuse of the id.</summary>
    public required string PayloadHash { get; init; }

    public PurchaseOrderStatus Status { get; set; }

    public DateTimeOffset AcceptedAt { get; init; }

    public static PurchaseOrderRecord Accept(PurchaseOrder order, string payloadHash, DateTimeOffset acceptedAt) => new()
    {
        Id = order.Id,
        CustomerId = order.CustomerId,
        Total = order.Total,
        Lines = order.Lines,
        PayloadHash = payloadHash,
        Status = PurchaseOrderStatus.Processing,
        AcceptedAt = acceptedAt,
    };
}

/// <summary>A generated shipping slip (BR2): one per order, keyed by the order id.</summary>
public sealed class ShippingSlipRecord
{
    public required PurchaseOrderId PurchaseOrderId { get; init; }

    public required CustomerId CustomerId { get; init; }

    public required IReadOnlyList<ProductLine> Items { get; init; }

    public DateTimeOffset GeneratedAt { get; init; }
}
