using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace FunBooksAndVideos.Api.Http;

/// <summary>The OpenAPI document: the controllers' XML comments, plus an overview and the brief's order as an example.</summary>
internal static class ApiDocs
{
    private const string Description = """
        Submit purchase orders and follow their fulfilment. A membership in an order is active by the time the
        submission answers 201 (BR1); physical products get a shipping slip shortly afterwards (BR2). The order id
        is the idempotency key, so resubmitting an order is always safe.
        """;

    // The brief's own example, offered as the request body for a submission.
    private const string BriefExample = """
        {
          "id": "3344656",
          "customerId": "4567890",
          "total": 48.50,
          "items": [
            { "type": "video", "title": "Comprehensive First Aid Training" },
            { "type": "book", "title": "The Girl on the train" },
            { "type": "membership", "membership": "bookClub" }
          ]
        }
        """;

    // A lambda, not a method group: the source generator that adds the XML comments doesn't see AddOpenApi called with one.
    public static IServiceCollection AddApiDocs(this IServiceCollection services) => services.AddOpenApi(openApi =>
    {
        openApi.AddDocumentTransformer((document, _, _) =>
        {
            document.Info = new OpenApiInfo { Title = "FunBooksAndVideos", Version = "v1", Description = Description };
            return Task.CompletedTask;
        });

        openApi.AddOperationTransformer((operation, context, _) =>
        {
            if (context.Description is { HttpMethod: "POST", RelativePath: "v1/purchase-orders" }
                && operation.RequestBody?.Content?.TryGetValue("application/json", out var body) == true)
                body.Example = JsonNode.Parse(BriefExample);
            return Task.CompletedTask;
        });
    });
}
