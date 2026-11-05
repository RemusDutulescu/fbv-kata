using System.Diagnostics;
using FunBooksAndVideos.Api.Contracts;
using FunBooksAndVideos.Api.Http;
using FunBooksAndVideos.Api.Validation;
using FunBooksAndVideos.Application.Queries;
using FunBooksAndVideos.Application.Submission;
using FunBooksAndVideos.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FunBooksAndVideos.Api.Controllers;

[ApiController]
[Route("v1/purchase-orders")]
[Tags("Purchase orders")]
public sealed class PurchaseOrdersController(OrderSubmitter submitter, ShopQueries queries) : ControllerBase
{
    /// <summary>Submit a purchase order.</summary>
    /// <remarks>
    /// Memberships in the order are active by the time this answers 201 or 200 (BR1). Physical products get a shipping
    /// slip shortly afterwards (BR2): it appears on the order. The order id is the idempotency key, so retrying is always safe.
    /// </remarks>
    /// <response code="201">Accepted, and its memberships are already active.</response>
    /// <response code="200">A retry of an order already stored, and its memberships are active. The body is the order as it stands now.</response>
    /// <response code="202">Accepted, but activation is taking longer than usual. Poll the Location, or resubmit: both are safe.</response>
    /// <response code="400">The order is invalid. Nothing was processed.</response>
    /// <response code="409">The id is already taken: by a different order, or by an earlier submission that can't run again.</response>
    /// <response code="429">Too many submissions. Retry in a minute.</response>
    /// <response code="503">Not confirmed in time, so it is unknown whether the order was received. Resubmit it: that is safe.</response>
    [HttpPost]
    [EnableRateLimiting(RateLimitingOptions.SubmissionsPolicy)]
    [Validate<SubmitPurchaseOrderRequest>]
    [ProducesResponseType<PurchaseOrderResponse>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<PurchaseOrderResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, "application/problem+json")]
    public async Task<IActionResult> SubmitAsync(SubmitPurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        var order = request.ToDomain();
        var outcome = await submitter.SubmitAsync(order, cancellationToken);
        var stored = await queries.FindOrderAsync(order.Id, cancellationToken);

        // One status per outcome.
        return (outcome, stored) switch
        {
            (SubmissionOutcome.Created, { } found) => Created(LocationOf(order.Id), found.ToResponse()),
            (SubmissionOutcome.Replayed, { } found) => Ok(found.ToResponse()),
            (SubmissionOutcome.Pending, _) => Accepted(LocationOf(order.Id)),

            // The stored order is never echoed here: it belongs to whoever used the id first.
            (SubmissionOutcome.Conflict, _) => Problem(
                statusCode: StatusCodes.Status409Conflict,
                detail: $"Purchase order id {order.Id} is already taken: by a different order, or by an earlier submission that can't run again."),

            (SubmissionOutcome.Unconfirmed, _) => Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                detail: $"Purchase order {order.Id} could not be confirmed in time. Resubmitting it is safe."),

            _ => throw new UnreachableException($"{outcome} has no stored order."),
        };
    }

    /// <summary>Get a purchase order and where its fulfilment stands.</summary>
    /// <param name="id">The purchase order id.</param>
    /// <response code="200">The order, with its shipping slip once one is generated.</response>
    /// <response code="400">The id is blank or longer than 64 characters.</response>
    /// <response code="404">No order has this id.</response>
    [HttpGet("{id}")]
    [ProducesResponseType<PurchaseOrderResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<PurchaseOrderResponse>> GetAsync(string id, CancellationToken cancellationToken) =>
        await queries.FindOrderAsync(new PurchaseOrderId(id), cancellationToken) is { } stored
            ? Ok(stored.ToResponse())
            : NotFound();

    private static string LocationOf(PurchaseOrderId id) => $"/v1/purchase-orders/{id}";
}
