using FunBooksAndVideos.Domain;
using FunBooksAndVideos.Domain.Fulfilment;
using FunBooksAndVideos.Infrastructure.Json;

namespace FunBooksAndVideos.IntegrationTests;

public sealed class DomainJsonTests
{
    /// <summary>
    /// The one step of adding a command or an item that the compiler can't check. Without a JSON name, the workflow
    /// can't record it, and every order it appears in is retried forever.
    /// </summary>
    [Theory]
    [InlineData(typeof(ItemLine))]
    [InlineData(typeof(ProductLine))]
    [InlineData(typeof(FulfilmentCommand))]
    public void Every_domain_subtype_has_a_json_name(Type baseType)
    {
        var named = DomainJson.Options.GetTypeInfo(baseType).PolymorphismOptions!.DerivedTypes.Select(derived => derived.DerivedType);
        var concrete = baseType.Assembly.GetTypes().Where(type => type.IsSubclassOf(baseType) && !type.IsAbstract);

        Assert.Equal(concrete.OrderBy(type => type.Name), named.OrderBy(type => type.Name));
    }
}
