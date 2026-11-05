using FunBooksAndVideos.Domain.Fulfilment;
using FunBooksAndVideos.Domain.Rules;

namespace FunBooksAndVideos.Domain.Tests;

public sealed class PurchaseOrderProcessorTests
{
    [Fact]
    public void The_brief_example_activates_a_membership_and_ships_the_book()
    {
        var processor = new PurchaseOrderProcessor([new MembershipActivationRule(), new ShippingSlipRule()]);

        var commands = processor.Process(PurchaseOrders.BriefExample());

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

        var commands = processor.Process(PurchaseOrders.APurchaseOrder().WithVideo().Build());

        Assert.Equal(extra, Assert.Single(commands));
    }

    [Fact]
    public async Task Each_command_dispatches_itself_to_its_receiver_method()
    {
        var receiver = new RecordingFulfilment();
        var processor = new PurchaseOrderProcessor([new MembershipActivationRule(), new ShippingSlipRule()]);

        foreach (var command in processor.Process(PurchaseOrders.BriefExample()))
            await command.ExecuteAsync(receiver);

        Assert.Equal([nameof(IFulfilment.ActivateMembershipAsync), nameof(IFulfilment.GenerateShippingSlipAsync)], receiver.Calls);
    }

    private sealed class StubRule(FulfilmentCommand command) : IPurchaseOrderRule
    {
        public string Name => "stub";

        public IEnumerable<FulfilmentCommand> Evaluate(PurchaseOrder order) => [command];
    }

    private sealed class RecordingFulfilment : IFulfilment
    {
        public List<string> Calls { get; } = [];

        public Task ActivateMembershipAsync(ActivateMembership command)
        {
            Calls.Add(nameof(ActivateMembershipAsync));
            return Task.CompletedTask;
        }

        public Task GenerateShippingSlipAsync(GenerateShippingSlip command)
        {
            Calls.Add(nameof(GenerateShippingSlipAsync));
            return Task.CompletedTask;
        }
    }
}
