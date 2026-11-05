using FunBooksAndVideos.Domain.Fulfilment;

namespace FunBooksAndVideos.Domain.Rules;

/// <summary>BR1. If the purchase order contains a membership, it has to be activated in the customer account immediately.</summary>
public sealed class MembershipActivationRule : IPurchaseOrderRule
{
    public string Name => "BR1 membership activation";

    public IEnumerable<FulfilmentCommand> Evaluate(PurchaseOrder order)
    {
        ArgumentNullException.ThrowIfNull(order);

        // One activation for everything requested: book-club and video-club lines together activate Premium, once.
        var requested = order.Lines
            .OfType<MembershipLine>()
            .Aggregate(MembershipType.None, (all, line) => all | line.Membership);

        return requested == MembershipType.None
            ? []
            : [new ActivateMembership(order.CustomerId, requested, Timing.Immediate)];
    }
}
