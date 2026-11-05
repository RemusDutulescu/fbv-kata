using FunBooksAndVideos.Api.Contracts;
using FunBooksAndVideos.Api.Persistence;
using FunBooksAndVideos.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

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
        string id, ShopDbContext db, CancellationToken cancellationToken)
    {
        var customerId = new CustomerId(id);
        var memberships = await db.Customers.AsNoTracking()
            .Where(customer => customer.Id == customerId)
            .Select(customer => customer.Memberships)
            .SingleOrDefaultAsync(cancellationToken);

        return TypedResults.Ok(memberships.ToResponse(customerId));
    }
}
