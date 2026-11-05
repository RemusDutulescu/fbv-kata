namespace FunBooksAndVideos.Domain.Fulfilment;

/// <summary>When a command runs relative to the moment the order is acknowledged.</summary>
public enum Timing
{
    /// <summary>Done before the order is acknowledged (BR1 says "immediately").</summary>
    Immediate,

    /// <summary>Done durably after the order is acknowledged.</summary>
    Deferred,
}

/// <summary>
/// Command pattern. A rule (the client) decides what has to happen and says so with a command;
/// the workflow (the invoker) decides when it runs, honouring <see cref="Timing"/>;
/// an <see cref="IFulfilment"/> (the receiver) does the work.
/// </summary>
public abstract record FulfilmentCommand(Timing Timing)
{
    /// <summary>Double dispatch: each command picks its receiver method, so the invoker never switches on types.</summary>
    public abstract Task ExecuteAsync(IFulfilment receiver);
}

/// <summary>Add memberships to a customer's account.</summary>
public sealed record ActivateMembership(CustomerId CustomerId, MembershipType Membership, Timing Timing)
    : FulfilmentCommand(Timing)
{
    public override Task ExecuteAsync(IFulfilment receiver) => receiver.ActivateMembershipAsync(this);
}

/// <summary>Produce a shipping slip listing the physical products of an order.</summary>
public sealed record GenerateShippingSlip(
    PurchaseOrderId PurchaseOrderId,
    CustomerId CustomerId,
    IReadOnlyList<ProductLine> Items,
    Timing Timing)
    : FulfilmentCommand(Timing)
{
    public override Task ExecuteAsync(IFulfilment receiver) => receiver.GenerateShippingSlipAsync(this);
}
