using FluentValidation;

namespace FunBooksAndVideos.Api.Validation;

/// <summary>
/// Runs the request's validator before the handler, as a step in the endpoint pipeline. A chain of responsibility fits
/// here, unlike for the business rules: each step may end the request, and an invalid one never reaches the handler.
/// </summary>
public sealed class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (context.Arguments.OfType<T>().FirstOrDefault() is not { } request)
            return await next(context);

        var result = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);
        return result.IsValid ? await next(context) : TypedResults.ValidationProblem(result.ToDictionary());
    }
}
