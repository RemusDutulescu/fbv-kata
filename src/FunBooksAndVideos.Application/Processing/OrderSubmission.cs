using FunBooksAndVideos.Domain;

namespace FunBooksAndVideos.Application.Processing;

/// <summary>
/// The workflow's input: a validated order as plain data. It is its own type, separate from the HTTP contract,
/// so the API can change shape without breaking the replay of workflows already running.
/// </summary>
public sealed record OrderSubmission(PurchaseOrderId Id, CustomerId CustomerId, decimal Total, IReadOnlyList<ItemLine> Lines)
{
    public static OrderSubmission From(PurchaseOrder order) => new(order.Id, order.CustomerId, order.Total.Amount, order.Lines);

    public PurchaseOrder ToPurchaseOrder() => new(Id, CustomerId, new Money(Total), Lines);
}
