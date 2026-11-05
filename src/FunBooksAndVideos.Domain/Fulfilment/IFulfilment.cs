namespace FunBooksAndVideos.Domain.Fulfilment;

/// <summary>
/// The receiver of <see cref="FulfilmentCommand"/>s: everything fulfilment can do. A new kind of effect
/// is a new method here, and the solution doesn't compile until every receiver implements it.
/// </summary>
public interface IFulfilment
{
    Task ActivateMembershipAsync(ActivateMembership command);

    Task GenerateShippingSlipAsync(GenerateShippingSlip command);
}
