namespace FunBooksAndVideos.Domain.Tests;

public sealed class CustomerTests
{
    [Fact]
    public void A_new_customer_has_no_memberships() =>
        Assert.Equal(MembershipType.None, NewCustomer().Memberships);

    [Fact]
    public void Activating_both_clubs_makes_the_customer_premium()
    {
        var customer = NewCustomer();

        customer.Activate(MembershipType.BookClub);
        customer.Activate(MembershipType.VideoClub);

        Assert.Equal(MembershipType.Premium, customer.Memberships);
    }

    [Fact]
    public void Activating_a_membership_twice_changes_nothing()
    {
        var customer = NewCustomer(MembershipType.BookClub);

        customer.Activate(MembershipType.BookClub);

        Assert.Equal(MembershipType.BookClub, customer.Memberships);
    }

    [Fact]
    public void Premium_includes_a_club_the_customer_already_has()
    {
        var customer = NewCustomer(MembershipType.VideoClub);

        customer.Activate(MembershipType.Premium);

        Assert.Equal(MembershipType.Premium, customer.Memberships);
    }

    [Fact]
    public void Only_real_memberships_can_be_activated() =>
        Assert.Throws<DomainException>(() => NewCustomer().Activate(MembershipType.None));

    private static Customer NewCustomer(MembershipType memberships = MembershipType.None) =>
        new(new CustomerId("4567890"), memberships);
}
