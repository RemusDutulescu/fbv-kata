using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace FunBooksAndVideos.Infrastructure.Json;

/// <summary>
/// Strategy for one class hierarchy: it writes a value with the name of its type first, as "type", and reads a value
/// back as the type that name stands for. A converter applies only to the exact type it is for, the hierarchy's base,
/// so each concrete type underneath is written and read as usual.
/// </summary>
internal abstract class DiscriminatedJson<T> : JsonConverter<T>
    where T : class
{
    private const string Discriminator = "type";

    /// <summary>The name a value's runtime type is written under.</summary>
    protected abstract string NameOf(T value);

    /// <summary>The concrete type a name stands for.</summary>
    protected abstract Type TypeOf(string name);

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var json = JsonNode.Parse(ref reader)!.AsObject();
        var name = (string?)json[Discriminator] ?? throw new JsonException($"A {typeof(T).Name} needs a \"{Discriminator}\".");
        json.Remove(Discriminator);
        return (T)json.Deserialize(TypeOf(name), options)!;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        var json = JsonSerializer.SerializeToNode(value, value.GetType(), options)!.AsObject();
        json.Insert(0, Discriminator, NameOf(value));
        json.WriteTo(writer, options);
    }
}
