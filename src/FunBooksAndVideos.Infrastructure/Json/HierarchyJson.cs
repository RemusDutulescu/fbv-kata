using System.Text.Json;
using FunBooksAndVideos.Domain;
using FunBooksAndVideos.Domain.Fulfilment;

namespace FunBooksAndVideos.Infrastructure.Json;

// One strategy per class hierarchy in the domain: the value's type decides its name, and the name decides the type.
// A new subtype needs a line in both switches of each hierarchy it belongs to, and a test fails until it has them.

internal sealed class ItemLineJson : DiscriminatedJson<ItemLine>
{
    protected override string NameOf(ItemLine line) => line switch
    {
        BookLine => "book",
        VideoLine => "video",
        MembershipLine => "membership",
        _ => throw new JsonException($"{line.GetType().Name} has no JSON name."),
    };

    protected override Type TypeOf(string name) => name switch
    {
        "book" => typeof(BookLine),
        "video" => typeof(VideoLine),
        "membership" => typeof(MembershipLine),
        _ => throw new JsonException($"'{name}' is not an item line."),
    };
}

internal sealed class ProductLineJson : DiscriminatedJson<ProductLine>
{
    protected override string NameOf(ProductLine line) => line switch
    {
        BookLine => "book",
        VideoLine => "video",
        _ => throw new JsonException($"{line.GetType().Name} has no JSON name."),
    };

    protected override Type TypeOf(string name) => name switch
    {
        "book" => typeof(BookLine),
        "video" => typeof(VideoLine),
        _ => throw new JsonException($"'{name}' is not a product."),
    };
}

internal sealed class FulfilmentCommandJson : DiscriminatedJson<FulfilmentCommand>
{
    protected override string NameOf(FulfilmentCommand command) => command switch
    {
        ActivateMembership => "activateMembership",
        GenerateShippingSlip => "generateShippingSlip",
        _ => throw new JsonException($"{command.GetType().Name} has no JSON name."),
    };

    protected override Type TypeOf(string name) => name switch
    {
        "activateMembership" => typeof(ActivateMembership),
        "generateShippingSlip" => typeof(GenerateShippingSlip),
        _ => throw new JsonException($"'{name}' is not a fulfilment command."),
    };
}
