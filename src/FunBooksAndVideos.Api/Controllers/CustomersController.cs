using FunBooksAndVideos.Api.Contracts;
using FunBooksAndVideos.Application.Queries;
using FunBooksAndVideos.Domain;
using Microsoft.AspNetCore.Mvc;

namespace FunBooksAndVideos.Api.Controllers;

[ApiController]
[Route("v1/customers")]
[Tags("Customers")]
public sealed class CustomersController(ShopQueries queries) : ControllerBase
{
    /// <summary>Get the clubs a customer belongs to.</summary>
    /// <param name="id">The customer id.</param>
    /// <response code="200">The customer's clubs. A customer this service has never seen belongs to none: an answer, not a 404.</response>
    /// <response code="400">The id is blank or longer than 64 characters.</response>
    [HttpGet("{id}/memberships")]
    [ProducesResponseType<MembershipsResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<MembershipsResponse> GetMembershipsAsync(string id, CancellationToken cancellationToken)
    {
        var customerId = new CustomerId(id);
        return (await queries.MembershipsOfAsync(customerId, cancellationToken)).ToResponse(customerId);
    }
}
