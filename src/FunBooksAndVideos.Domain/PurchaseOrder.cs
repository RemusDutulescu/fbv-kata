namespace FunBooksAndVideos.Domain;

/// <summary>
/// Aggregate root for a purchase order. Always valid: every invariant is checked here,
/// so an invalid order can't exist and no rule ever has to re-check its input.
/// </summary>
public sealed class PurchaseOrder
{
    public const int MaxLines = 100;

    public PurchaseOrder(PurchaseOrderId id, CustomerId customerId, Money total, IEnumerable<ItemLine> lines)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(customerId);
        ArgumentNullException.ThrowIfNull(lines);

        // The order owns its lines: later changes to the caller's collection can't reach it.
        var copy = lines.ToArray();
        if (copy.Length == 0) throw new DomainException("A purchase order needs at least one item line.");
        if (copy.Length > MaxLines) throw new DomainException($"A purchase order has at most {MaxLines} item lines.");
        if (Array.Exists(copy, line => line is null)) throw new DomainException("An item line is missing.");

        Id = id;
        CustomerId = customerId;
        Total = total;
        Lines = copy.AsReadOnly();
    }

    public PurchaseOrderId Id { get; }

    public CustomerId CustomerId { get; }

    public Money Total { get; }

    public IReadOnlyList<ItemLine> Lines { get; }
}
