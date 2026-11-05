using System.Text.Json;
using System.Text.Json.Serialization;
using FunBooksAndVideos.Domain;

namespace FunBooksAndVideos.Infrastructure.Json;

/// <summary>
/// JSON for domain types, used by the JSON columns and by Temporal payloads. Each class hierarchy has a converter of
/// its own here, so the domain stays free of serialization attributes.
/// </summary>
public static class DomainJson
{
    public static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters =
            {
                new ItemLineJson(),
                new ProductLineJson(),
                new FulfilmentCommandJson(),
                new JsonStringEnumConverter(),
                new IdConverter<PurchaseOrderId>(id => id.Value, value => new PurchaseOrderId(value)),
                new IdConverter<CustomerId>(id => id.Value, value => new CustomerId(value)),
            },
        };
        options.MakeReadOnly(populateMissingResolver: true);
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
