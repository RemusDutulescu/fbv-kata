using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using FunBooksAndVideos.Api.Contracts;

namespace FunBooksAndVideos.Api.Http;

/// <summary>The first validation layer: a body that doesn't match the contract's shape never reaches an action.</summary>
internal static class StrictJson
{
    public static void Apply(JsonSerializerOptions json)
    {
        json.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        json.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow; // a misspelt field fails instead of being dropped
        json.RespectRequiredConstructorParameters = true;                  // a missing one fails instead of defaulting to 0
        json.RespectNullableAnnotations = true;                            // and so does null where the contract has no '?'
        json.AllowOutOfOrderMetadataProperties = true;                     // "type" may come anywhere: client serializers differ
        json.NumberHandling = JsonNumberHandling.Strict;                   // "48.50" in quotes is a string, not a total
        json.AllowDuplicateProperties = false;                             // one value per field, not the last one sent

        // A line without "type" would reach the abstract base and fail as NotSupportedException: a 500 for a client's mistake.
        json.TypeInfoResolver = json.TypeInfoResolver!.WithAddedModifier(type =>
        {
            if (type.Type == typeof(ItemLineDto))
                type.CreateObject = () => throw new JsonException("Each item needs a type: book, video or membership.");
        });
    }
}
