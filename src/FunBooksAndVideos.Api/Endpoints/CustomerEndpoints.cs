using FunBooksAndVideos.Api.Contracts;
using FunBooksAndVideos.Application.Queries;
using FunBooksAndVideos.Domain;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FunBooksAndVideos.Api.Endpoints;

public static class CustomerEndpoints
{
    public static RouteGroupBuilder MapCustomers(this RouteGroupBuilder v1)
    {
        v1.MapGet("/customers/{id}/memberships", GetMembershipsAsync)
            .WithTags("Customers")
            .ProducesProblem(StatusCodes.Status400BadRequest);
        return v1;
    }

    /// <summary>Get the clubs a customer belongs to.</summary>
    /// <param name="id">The customer id.</param>
    /// <response code="200">The customer's clubs. A customer this service has never seen belongs to none: an answer, not a 404.</response>
    /// <response code="400">The id is blank or longer than 64 characters.</response>
    internal static async Task<Ok<MembershipsResponse>> GetMembershipsAsync(
        string id, ShopQueries queries, CancellationToken cancellationToken)
    {
        var customerId = new CustomerId(id);
        return TypedResults.Ok((await queries.MembershipsOfAsync(customerId, cancellationToken)).ToResponse(customerId));
    }
}
