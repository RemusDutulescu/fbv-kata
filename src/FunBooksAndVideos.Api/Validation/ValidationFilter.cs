using FluentValidation;

namespace FunBooksAndVideos.Api.Validation;

/// <summary>Runs the request's validator as a step in the endpoint pipeline: an invalid request never reaches the handler.</summary>
public sealed class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await validator.ValidateAsync(context.Arguments.OfType<T>().Single(), context.HttpContext.RequestAborted);
        return result.IsValid ? await next(context) : TypedResults.ValidationProblem(result.ToDictionary());
    }
}
