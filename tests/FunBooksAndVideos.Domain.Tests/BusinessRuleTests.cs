using FunBooksAndVideos.Domain.Fulfilment;
using FunBooksAndVideos.Domain.Rules;
using static FunBooksAndVideos.Domain.Tests.PurchaseOrders;

namespace FunBooksAndVideos.Domain.Tests;

public sealed class BusinessRuleTests
{
    private static readonly CustomerId BriefCustomer = new("4567890");

    [Fact]
    public void BR1_activates_the_ordered_membership_immediately()
    {
        var commands = new MembershipActivationRule().Evaluate(BriefExample());

        Assert.Equal(new ActivateMembership(BriefCustomer, MembershipType.BookClub, Timing.Immediate), Assert.Single(commands));
    }

    [Fact]
    public void BR1_does_nothing_without_a_membership() =>
        Assert.Empty(new MembershipActivationRule().Evaluate(AnOrder(new BookLine("Dune"), new VideoLine("Alien"))));

    [Fact]
    public void BR1_activates_both_clubs_as_premium_in_one_go()
    {
        var order = AnOrder(
            new MembershipLine(MembershipType.BookClub),
            new MembershipLine(MembershipType.VideoClub),
            new MembershipLine(MembershipType.BookClub));

        var commands = new MembershipActivationRule().Evaluate(order);

        Assert.Equal(new ActivateMembership(order.CustomerId, MembershipType.Premium, Timing.Immediate), Assert.Single(commands));
    }

    [Fact]
    public void BR2_puts_only_physical_products_on_the_shipping_slip()
    {
        var commands = new ShippingSlipRule().Evaluate(BriefExample());

        var slip = Assert.IsType<GenerateShippingSlip>(Assert.Single(commands));
        Assert.Equal(new PurchaseOrderId("3344656"), slip.PurchaseOrderId);
        Assert.Equal(BriefCustomer, slip.CustomerId);
        Assert.Equal(new BookLine("The Girl on the train"), Assert.Single(slip.Items));
        Assert.Equal(Timing.Deferred, slip.Timing);
    }

    [Fact]
    public void BR2_lists_every_physical_product_on_one_slip()
    {
        var order = AnOrder(new BookLine("Dune"), new BookLine("Emma"));

        var slip = Assert.IsType<GenerateShippingSlip>(Assert.Single(new ShippingSlipRule().Evaluate(order)));

        Assert.Equal<ProductLine>([new BookLine("Dune"), new BookLine("Emma")], slip.Items);
    }

    [Fact]
    public void BR2_does_nothing_when_nothing_ships() =>
        Assert.Empty(new ShippingSlipRule().Evaluate(AnOrder(new VideoLine("Alien"), new MembershipLine(MembershipType.VideoClub))));
}
