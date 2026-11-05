using FunBooksAndVideos.Domain.Fulfilment;

namespace FunBooksAndVideos.Domain.Rules;

/// <summary>
/// Strategy: one business rule. Pure: it reads the order and says what has to happen, but never does it,
/// so it can be tested without any infrastructure and evaluated anywhere.
/// </summary>
public interface IPurchaseOrderRule
{
    /// <summary>A stable name for logs and traces.</summary>
    string Name { get; }

    IEnumerable<FulfilmentCommand> Evaluate(PurchaseOrder order);
}
