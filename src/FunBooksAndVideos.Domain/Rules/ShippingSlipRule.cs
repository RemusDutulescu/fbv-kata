using FunBooksAndVideos.Domain.Fulfilment;

namespace FunBooksAndVideos.Domain.Rules;

/// <summary>BR2. If the purchase order contains a physical product, a shipping slip has to be generated.</summary>
public sealed class ShippingSlipRule : IPurchaseOrderRule
{
    public string Name => "BR2 shipping slip";

    public IEnumerable<FulfilmentCommand> Evaluate(PurchaseOrder order)
    {
        var physical = order.Lines.OfType<ProductLine>().Where(line => line.IsPhysical).ToArray();

        // BR2 doesn't ask for immediacy, so the slip is produced after the order is acknowledged.
        return physical.Length == 0
            ? []
            : [new GenerateShippingSlip(order.Id, order.CustomerId, physical.AsReadOnly(), Timing.Deferred)];
    }
}
