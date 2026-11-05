using FunBooksAndVideos.Domain.Fulfilment;
using FunBooksAndVideos.Domain.Rules;

namespace FunBooksAndVideos.Domain.Tests;

public sealed class BusinessRuleTests
{
    private static readonly CustomerId BriefCustomer = new("4567890");

    [Fact]
    public void BR1_activates_the_ordered_membership_immediately()
    {
        var commands = new MembershipActivationRule().Evaluate(PurchaseOrders.BriefExample());

        Assert.Equal(new ActivateMembership(BriefCustomer, MembershipType.BookClub, Timing.Immediate), Assert.Single(commands));
    }

    [Fact]
    public void BR1_does_nothing_without_a_membership() =>
        Assert.Empty(new MembershipActivationRule().Evaluate(PurchaseOrders.APurchaseOrder().WithBook().WithVideo().Build()));

    [Fact]
    public void BR1_activates_both_clubs_as_premium_in_one_go()
    {
        var order = PurchaseOrders.APurchaseOrder()
            .ForCustomer("4567890")
            .WithMembership(MembershipType.BookClub)
            .WithMembership(MembershipType.VideoClub)
            .WithMembership(MembershipType.BookClub)
            .Build();

        var commands = new MembershipActivationRule().Evaluate(order);

        Assert.Equal(new ActivateMembership(BriefCustomer, MembershipType.Premium, Timing.Immediate), Assert.Single(commands));
    }

    [Fact]
    public void BR2_puts_only_physical_products_on_the_shipping_slip()
    {
        var commands = new ShippingSlipRule().Evaluate(PurchaseOrders.BriefExample());

        var slip = Assert.IsType<GenerateShippingSlip>(Assert.Single(commands));
        Assert.Equal(new PurchaseOrderId("3344656"), slip.PurchaseOrderId);
        Assert.Equal(BriefCustomer, slip.CustomerId);
        Assert.Equal(new BookLine("The Girl on the train"), Assert.Single(slip.Items));
        Assert.Equal(Timing.Deferred, slip.Timing);
    }

    [Fact]
    public void BR2_lists_every_physical_product_on_one_slip()
    {
        var order = PurchaseOrders.APurchaseOrder().WithBook("Dune").WithBook("Emma").Build();

        var slip = Assert.IsType<GenerateShippingSlip>(Assert.Single(new ShippingSlipRule().Evaluate(order)));

        Assert.Equal<ProductLine>([new BookLine("Dune"), new BookLine("Emma")], slip.Items);
    }

    [Fact]
    public void BR2_does_nothing_when_nothing_ships()
    {
        var order = PurchaseOrders.APurchaseOrder().WithVideo().WithMembership(MembershipType.VideoClub).Build();

        Assert.Empty(new ShippingSlipRule().Evaluate(order));
    }
}
