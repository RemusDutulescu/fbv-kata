using System.Text.Json;
using FunBooksAndVideos.Domain;
using FunBooksAndVideos.Domain.Fulfilment;
using FunBooksAndVideos.Infrastructure.Json;

namespace FunBooksAndVideos.IntegrationTests;

public sealed class DomainJsonTests
{
    private static readonly CustomerId Customer = new("4567890");

    /// <summary>A value of every concrete domain type, under each hierarchy it is written as.</summary>
    public static TheoryData<Type, object> Values => new()
    {
        { typeof(ItemLine), new BookLine("Dune") },
        { typeof(ItemLine), new VideoLine("Dune") },
        { typeof(ItemLine), new MembershipLine(MembershipType.BookClub) },
        { typeof(ProductLine), new BookLine("Dune") },
        { typeof(ProductLine), new VideoLine("Dune") },
        { typeof(FulfilmentCommand), new ActivateMembership(Customer, MembershipType.BookClub, Timing.Immediate) },
        { typeof(FulfilmentCommand), new GenerateShippingSlip(new PurchaseOrderId("3344656"), Customer, [new BookLine("Dune")], Timing.Deferred) },
    };

    /// <summary>
    /// The one step of adding a command or an item that the compiler can't check. Without a JSON name, the workflow
    /// can't record it, and every order it appears in is retried forever.
    /// </summary>
    [Theory]
    [InlineData(typeof(ItemLine))]
    [InlineData(typeof(ProductLine))]
    [InlineData(typeof(FulfilmentCommand))]
    public void Every_domain_subtype_has_a_value_above(Type hierarchy)
    {
        var concrete = hierarchy.Assembly.GetTypes().Where(type => type.IsSubclassOf(hierarchy) && !type.IsAbstract);
        var covered = Values.Select(row => row.Data).Where(data => data.Item1 == hierarchy).Select(data => data.Item2.GetType());

        Assert.Equal(concrete.OrderBy(type => type.Name), covered.OrderBy(type => type.Name));
    }

    [Theory]
    [MemberData(nameof(Values))]
    public void Every_domain_subtype_reads_back_by_its_json_name(Type hierarchy, object value)
    {
        var json = JsonSerializer.Serialize(value, hierarchy, DomainJson.Options);

        var read = JsonSerializer.Deserialize(json, hierarchy, DomainJson.Options)!;

        Assert.IsType(value.GetType(), read);
        Assert.Equal(json, JsonSerializer.Serialize(read, hierarchy, DomainJson.Options));
    }
}
