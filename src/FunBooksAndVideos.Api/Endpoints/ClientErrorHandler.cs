using System.Text.Json;
using FunBooksAndVideos.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FunBooksAndVideos.Api.Endpoints;

/// <summary>
/// A client's mistake becomes a problem response that says what was wrong. Anything else is left to the default
/// handler: a 500 that says nothing about the server.
/// </summary>
internal sealed class ClientErrorHandler(IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            // The body doesn't match the contract's shape; the path says where, e.g. $.items[0].membership.
            BadHttpRequestException { InnerException: JsonException json } => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Detail = json.Message,
                Extensions = { ["path"] = json.Path },
            },

            // No body, too big a body: the status says it all, and the framework's wording is meant for developers.
            BadHttpRequestException badRequest => new ProblemDetails { Status = badRequest.StatusCode },

            // The domain refused the input: a blank or over-long id in a URL, or anything validation missed.
            DomainException domain => new ProblemDetails { Status = StatusCodes.Status400BadRequest, Detail = domain.Message },
            _ => null,
        };
        if (problem is null) return false;

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status400BadRequest;
        return await problems.TryWriteAsync(new() { HttpContext = httpContext, Exception = exception, ProblemDetails = problem });
    }
}
