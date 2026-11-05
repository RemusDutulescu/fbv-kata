using FunBooksAndVideos.Domain.Fulfilment;

namespace FunBooksAndVideos.Domain.Rules;

/// <summary>
/// Applies every business rule to an order and collects what has to happen.
/// Open/Closed: a new rule is a new <see cref="IPurchaseOrderRule"/> registered in DI; this class never changes.
/// Rules are independent and all of them apply, which is why this is not a chain of responsibility.
/// </summary>
public sealed class PurchaseOrderProcessor(IEnumerable<IPurchaseOrderRule> rules)
{
    public IReadOnlyList<FulfilmentCommand> Process(PurchaseOrder order)
    {
        ArgumentNullException.ThrowIfNull(order);

        return [.. rules.SelectMany(rule => rule.Evaluate(order))];
    }
}
