using FunBooksAndVideos.Domain.Fulfilment;
using FunBooksAndVideos.Domain.Rules;
using static FunBooksAndVideos.Domain.Tests.PurchaseOrders;

namespace FunBooksAndVideos.Domain.Tests;

public sealed class PurchaseOrderProcessorTests
{
    [Fact]
    public void The_brief_example_activates_a_membership_and_ships_the_book()
    {
        var processor = new PurchaseOrderProcessor([new MembershipActivationRule(), new ShippingSlipRule()]);

        var commands = processor.Process(BriefExample());

        Assert.Collection(
            commands,
            command => Assert.IsType<ActivateMembership>(command),
            command => Assert.IsType<GenerateShippingSlip>(command));
    }

    [Fact]
    public void A_new_rule_plugs_in_without_changing_the_processor()
    {
        var extra = new ActivateMembership(new CustomerId("2000"), MembershipType.VideoClub, Timing.Deferred);
        var processor = new PurchaseOrderProcessor([new ShippingSlipRule(), new StubRule(extra)]);

        var commands = processor.Process(AnOrder(new VideoLine("Alien")));

        Assert.Equal(extra, Assert.Single(commands));
    }

    private sealed class StubRule(FulfilmentCommand command) : IPurchaseOrderRule
    {
        public string Name => "stub";

        public IEnumerable<FulfilmentCommand> Evaluate(PurchaseOrder order) => [command];
    }
}
