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
        v1.MapGet("/customers/{id}/memberships", GetMembershipsAsync).WithTags("Customers");
        return v1;
    }

    /// <summary>Get the clubs a customer belongs to.</summary>
    /// <remarks>
    /// Customer accounts live in the shop; this service knows the memberships it has activated. A customer it has
    /// never seen belongs to no club, which is an answer rather than a 404.
    /// </remarks>
    /// <param name="id">The customer id.</param>
    /// <response code="200">The customer's clubs: none for a customer this service has never seen.</response>
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
