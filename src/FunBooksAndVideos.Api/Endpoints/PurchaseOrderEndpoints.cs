using System.Diagnostics;
using System.Text.Json.Nodes;
using FunBooksAndVideos.Api.Contracts;
using FunBooksAndVideos.Api.Persistence;
using FunBooksAndVideos.Api.Processing;
using FunBooksAndVideos.Api.Validation;
using FunBooksAndVideos.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace FunBooksAndVideos.Api.Endpoints;

public static class PurchaseOrderEndpoints
{
    // The brief's own example, offered as the request body in the API docs.
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

    public static RouteGroupBuilder MapPurchaseOrders(this RouteGroupBuilder v1)
    {
        var orders = v1.MapGroup("/purchase-orders").WithTags("Purchase orders");

        orders.MapPost("/", SubmitAsync)
            .AddEndpointFilter<ValidationFilter<SubmitPurchaseOrderRequest>>()
            .RequireRateLimiting(HttpApi.SubmissionsPolicy)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .AddOpenApiOperationTransformer((operation, _, _) =>
            {
                if (operation.RequestBody?.Content?.TryGetValue("application/json", out var body) == true)
                    body.Example = JsonNode.Parse(BriefExample);
                return Task.CompletedTask;
            });

        orders.MapGet("/{id}", GetAsync).ProducesProblem(StatusCodes.Status404NotFound);
        return v1;
    }

    /// <summary>Submit a purchase order.</summary>
    /// <remarks>
    /// Memberships in the order are active by the time this answers 201 (BR1). Physical products get a shipping slip
    /// shortly afterwards (BR2): it appears on the order. The order id is the idempotency key, so retrying is always safe.
    /// </remarks>
    /// <response code="201">Accepted, and its memberships are already active.</response>
    /// <response code="200">This exact order was submitted before (a retry). The body is the order as it stands now.</response>
    /// <response code="202">Accepted, but activation is taking longer than usual. Poll the Location.</response>
    /// <response code="400">The order is invalid. Nothing was processed.</response>
    /// <response code="409">The id belongs to a different order.</response>
    /// <response code="429">Too many submissions. Retry in a minute.</response>
    internal static async Task<Results<Created<PurchaseOrderResponse>, Ok<PurchaseOrderResponse>, Accepted, ProblemHttpResult>> SubmitAsync(
        SubmitPurchaseOrderRequest request, OrderSubmitter submitter, ShopDbContext db, CancellationToken cancellationToken)
    {
        var outcome = await submitter.SubmitAsync(request.ToDomain(), cancellationToken);
        return Respond(outcome, await FindAsync(db, outcome.Id, cancellationToken));
    }

    /// <summary>Get a purchase order and where its fulfilment stands.</summary>
    /// <param name="id">The purchase order id.</param>
    /// <response code="200">The order, with its shipping slip once one is generated.</response>
    /// <response code="404">No order has this id.</response>
    internal static async Task<Results<Ok<PurchaseOrderResponse>, NotFound>> GetAsync(
        string id, ShopDbContext db, CancellationToken cancellationToken) =>
        await FindAsync(db, new PurchaseOrderId(id), cancellationToken) is { } order
            ? TypedResults.Ok(order)
            : TypedResults.NotFound();

    /// <summary>One status per outcome. Pure, so every branch is unit tested, including a 202 that is hard to provoke.</summary>
    /// <param name="outcome">What the submission came to.</param>
    /// <param name="stored">The order stored under the outcome's id, if there is one yet.</param>
    public static Results<Created<PurchaseOrderResponse>, Ok<PurchaseOrderResponse>, Accepted, ProblemHttpResult> Respond(
        SubmissionOutcome outcome, PurchaseOrderResponse? stored) => (outcome, stored) switch
    {
        (SubmissionOutcome.Created, { } order) => TypedResults.Created(LocationOf(outcome.Id), order),
        (SubmissionOutcome.Replayed, { } order) => TypedResults.Ok(order),
        (SubmissionOutcome.Pending, _) => TypedResults.Accepted(LocationOf(outcome.Id)),

        // The stored order is never echoed here: it belongs to whoever used the id first.
        (SubmissionOutcome.Conflict, _) => TypedResults.Problem(
            statusCode: StatusCodes.Status409Conflict,
            detail: $"Purchase order {outcome.Id} was already submitted with different contents."),

        _ => throw new UnreachableException($"{outcome} has no stored order."),
    };

    private static async Task<PurchaseOrderResponse?> FindAsync(ShopDbContext db, PurchaseOrderId id, CancellationToken cancellationToken)
    {
        // One round trip for the order and, once generated, its shipping slip.
        var found = await db.PurchaseOrders.AsNoTracking()
            .Where(order => order.Id == id)
            .LeftJoin(db.ShippingSlips, order => order.Id, slip => slip.PurchaseOrderId, (order, slip) => new { order, slip })
            .SingleOrDefaultAsync(cancellationToken);

        return found?.order.ToResponse(found.slip);
    }

    private static string LocationOf(PurchaseOrderId id) => $"/v1/purchase-orders/{id}";
}
