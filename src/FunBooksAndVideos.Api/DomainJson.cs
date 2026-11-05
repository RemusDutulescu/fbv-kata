using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using FunBooksAndVideos.Domain;
using FunBooksAndVideos.Domain.Fulfilment;

namespace FunBooksAndVideos.Api;

/// <summary>
/// JSON for domain types, used by the JSON columns and by Temporal payloads. Polymorphism is configured
/// here rather than with attributes, so the domain stays free of serialization concerns.
/// </summary>
public static class DomainJson
{
    public static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { AddPolymorphism } },
            Converters =
            {
                new JsonStringEnumConverter(),
                new IdConverter<PurchaseOrderId>(id => id.Value, value => new PurchaseOrderId(value)),
                new IdConverter<CustomerId>(id => id.Value, value => new CustomerId(value)),
            },
        };
        options.MakeReadOnly();
        return options;
    }

    private static void AddPolymorphism(JsonTypeInfo info)
    {
        if (info.Type == typeof(ItemLine))
            info.PolymorphismOptions = Discriminated(("book", typeof(BookLine)), ("video", typeof(VideoLine)), ("membership", typeof(MembershipLine)));
        else if (info.Type == typeof(ProductLine))
            info.PolymorphismOptions = Discriminated(("book", typeof(BookLine)), ("video", typeof(VideoLine)));
        else if (info.Type == typeof(FulfilmentCommand))
            info.PolymorphismOptions = Discriminated(("activateMembership", typeof(ActivateMembership)), ("generateShippingSlip", typeof(GenerateShippingSlip)));
    }

    private static JsonPolymorphismOptions Discriminated(params (string Name, Type Type)[] derivedTypes)
    {
        var options = new JsonPolymorphismOptions { TypeDiscriminatorPropertyName = "type" };
        foreach (var (name, type) in derivedTypes) options.DerivedTypes.Add(new JsonDerivedType(type, name));
        return options;
    }

    /// <summary>Writes a strongly-typed id as its plain text, so payloads stay readable in the Temporal UI.</summary>
    private sealed class IdConverter<T>(Func<T, string> write, Func<string, T> read) : JsonConverter<T>
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            read(reader.GetString() ?? throw new JsonException($"{typeof(T).Name} can't be null."));

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
            writer.WriteStringValue(write(value));
    }
}
