using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using FluentValidation;
using FunBooksAndVideos.Api.Contracts;
using FunBooksAndVideos.Api.Validation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace FunBooksAndVideos.Api.Endpoints;

/// <summary>The HTTP surface: strict JSON, problem details, validation, limits, health checks, docs and the /v1 routes.</summary>
public static class HttpApi
{
    public const string SubmissionsPolicy = "submissions";

    private const string ReadyTag = "ready";

    private const string Description = """
        Submit purchase orders and follow their fulfilment. A membership in an order is active by the time the
        submission answers 201 (BR1); physical products get a shipping slip shortly afterwards (BR2). The order id
        is the idempotency key, so resubmitting an order is always safe.
        """;

    public static IServiceCollection AddHttpApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureHttpJsonOptions(options => StrictJson(options.SerializerOptions));

        // A body that fails to bind is thrown rather than answered with a bare 400, so ClientErrorHandler can say why.
        services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
        services.AddProblemDetails();
        services.AddExceptionHandler<ClientErrorHandler>();

        // Errors name the JSON fields a client sent ("items[0].title"), not the C# properties.
        ValidatorOptions.Global.PropertyNameResolver = ValidatorOptions.Global.DisplayNameResolver =
            (_, member, _) => member is null ? null : JsonNamingPolicy.CamelCase.ConvertName(member.Name);
        services.AddSingleton<IValidator<SubmitPurchaseOrderRequest>, SubmitPurchaseOrderRequestValidator>();

        // Every submission starts a workflow, and workflows cost Temporal Cloud actions: a public endpoint needs a ceiling.
        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.AddFixedWindowLimiter(SubmissionsPolicy, window =>
            {
                window.PermitLimit = configuration.GetValue("RateLimiting:SubmissionsPerMinute", 30);
                window.Window = TimeSpan.FromMinutes(1);
            });
        });

        services.AddHealthChecks().AddCheck<ReadinessCheck>("dependencies", tags: [ReadyTag]);

        services.AddOpenApi(openApi => openApi.AddDocumentTransformer((document, _, _) =>
        {
            document.Info = new OpenApiInfo { Title = "FunBooksAndVideos", Version = "v1", Description = Description };
            return Task.CompletedTask;
        }));

        return services;
    }

    public static void MapHttpApi(this WebApplication app)
    {
        app.MapGroup("/v1").MapPurchaseOrders().MapCustomers();

        // Liveness only asks whether the process answers; readiness also needs SQL Server and Temporal.
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains(ReadyTag) });

        // The docs are public in every environment: the deployed demo is there to be tried.
        app.MapOpenApi();
        app.MapScalarApiReference();
        app.MapGet("/", () => TypedResults.Redirect("/scalar/")).ExcludeFromDescription();
    }

    /// <summary>The first validation layer: a body that doesn't match the contract's shape never reaches a handler.</summary>
    private static void StrictJson(JsonSerializerOptions json)
    {
        json.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
        json.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow; // a misspelt field fails instead of being dropped
        json.RespectRequiredConstructorParameters = true;                  // a missing one fails instead of defaulting to 0
        json.RespectNullableAnnotations = true;                            // and so does null where the contract has no '?'
        json.AllowOutOfOrderMetadataProperties = true;                     // "type" may come anywhere: client serializers differ

        // A line without "type" would reach the abstract base and fail as NotSupportedException: a 500 for a client's mistake.
        json.TypeInfoResolver = json.TypeInfoResolver?.WithAddedModifier(type =>
        {
            if (type.Type == typeof(ItemLineDto))
                type.CreateObject = () => throw new JsonException("Each item needs a type: book, video or membership.");
        });
    }
}
