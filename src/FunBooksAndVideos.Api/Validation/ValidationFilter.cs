using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FunBooksAndVideos.Api.Validation;

/// <summary>Validates an action's <typeparamref name="T"/> argument with its validator before the action runs.</summary>
public sealed class ValidateAttribute<T>() : TypeFilterAttribute(typeof(ValidationFilter<T>));

/// <summary>
/// A step in MVC's filter pipeline: an invalid request never reaches the action. Its 400 has the same shape as the
/// one MVC gives a body that doesn't bind, with the errors keyed by JSON field.
/// </summary>
public sealed class ValidationFilter<T>(IValidator<T> validator) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var result = await validator.ValidateAsync(context.ActionArguments.Values.OfType<T>().Single(), context.HttpContext.RequestAborted);
        if (result.IsValid)
        {
            await next();
            return;
        }

        foreach (var error in result.Errors) context.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        context.Result = ((ControllerBase)context.Controller).ValidationProblem();
    }
}
