namespace FunBooksAndVideos.Domain;

/// <summary>
/// Aggregate root for a customer account. It owns its memberships, so the union logic lives here
/// and not in SQL; concurrent changes are caught by optimistic concurrency in the persistence layer.
/// </summary>
public sealed class Customer
{
    public Customer(CustomerId id, MembershipType memberships = MembershipType.None)
    {
        ArgumentNullException.ThrowIfNull(id);
        if (memberships != MembershipType.None && !memberships.IsValid())
            throw new DomainException($"'{memberships}' is not a set of memberships.");

        Id = id;
        Memberships = memberships;
    }

    public CustomerId Id { get; }

    public MembershipType Memberships { get; private set; }

    /// <summary>Adds a membership. Idempotent: activating one the customer already has changes nothing.</summary>
    public void Activate(MembershipType membership)
    {
        if (!membership.IsValid()) throw new DomainException($"'{membership}' is not a membership.");

        Memberships |= membership;
    }
}
