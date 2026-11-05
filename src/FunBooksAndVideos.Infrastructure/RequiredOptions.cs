using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FunBooksAndVideos.Infrastructure;

public static class RequiredOptions
{
    /// <summary>
    /// Binds a configuration section to its options class, registers it as <see cref="IOptions{TOptions}"/> and returns
    /// it, for the registrations that need a value straight away. A missing section, or a value the class's attributes
    /// reject, stops the app here, before it starts.
    /// </summary>
    /// <remarks>
    /// Binding ignores the C# <c>required</c> keyword, which only holds code that builds the options to it, so the
    /// attributes are what configuration is checked against.
    /// </remarks>
    public static TOptions AddRequiredOptions<TOptions>(this IServiceCollection services, IConfiguration configuration, string section)
        where TOptions : class
    {
        var options = configuration.GetRequiredSection(section).Get<TOptions>()!;

        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(options, new ValidationContext(options), errors, validateAllProperties: true))
            throw new InvalidOperationException($"Section '{section}' is invalid. {string.Join(" ", errors.Select(error => error.ErrorMessage))}");

        services.AddSingleton(Options.Create(options));
        return options;
    }
}
