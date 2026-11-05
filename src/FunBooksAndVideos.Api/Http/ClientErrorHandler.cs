using FunBooksAndVideos.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FunBooksAndVideos.Api.Http;

/// <summary>
/// A client's mistake that surfaces as an exception becomes a problem response that says what was wrong. Anything
/// else is left to the default handler: a 500 that says nothing about the server.
/// </summary>
internal sealed class ClientErrorHandler(IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            // Too big a body: the status says it all, and the framework's wording is meant for developers.
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
