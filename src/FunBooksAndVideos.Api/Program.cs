using System.Text.Json;
using FluentValidation;
using FunBooksAndVideos.Api.Contracts;
using FunBooksAndVideos.Api.Http;
using FunBooksAndVideos.Api.Validation;
using FunBooksAndVideos.Application.Queries;
using FunBooksAndVideos.Application.Submission;
using FunBooksAndVideos.Infrastructure;
using FunBooksAndVideos.Infrastructure.Observability;
using FunBooksAndVideos.Infrastructure.Persistence;
using FunBooksAndVideos.Infrastructure.Temporal;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// The tiers underneath, as the worker has them too.
builder.Services.AddShopDatabase(builder.Configuration);
builder.Services.AddTemporal(builder.Configuration);
builder.Services.AddTelemetry(builder.Configuration, "FunBooksAndVideos.Api")
    // Probes call every few seconds; their traces would be noise, and they'd cost ingestion.
    .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation(http => http.Filter = context => !context.Request.Path.StartsWithSegments("/health")));

// Submissions go to Temporal, where a worker picks them up; reads come straight from SQL.
builder.Services.AddRequiredOptions<SubmissionOptions>(builder.Configuration, SubmissionOptions.Section);
builder.Services.AddScoped<OrderSubmitter>();
builder.Services.AddScoped<ShopQueries>();

// The controllers, with strict JSON. The OpenAPI document reads the minimal-API JSON settings, so it gets them too.
// What is required is the contract's to say (StrictJson, the validator); MVC's own guess adds "the request field is
// required" to every 400 for a body that doesn't bind.
builder.Services.AddControllers(mvc => mvc.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddJsonOptions(json => StrictJson.Apply(json.JsonSerializerOptions));
builder.Services.ConfigureHttpJsonOptions(json => StrictJson.Apply(json.SerializerOptions));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ClientErrorHandler>();

// Errors name the JSON fields a client sent ("items[0].title"), not the C# properties.
ValidatorOptions.Global.PropertyNameResolver = ValidatorOptions.Global.DisplayNameResolver =
    (_, member, _) => member is null ? null : JsonNamingPolicy.CamelCase.ConvertName(member.Name);
builder.Services.AddSingleton<IValidator<SubmitPurchaseOrderRequest>, SubmitPurchaseOrderRequestValidator>();

// One window shared by every client (ADR 6).
var limits = builder.Services.AddRequiredOptions<RateLimitingOptions>(builder.Configuration, RateLimitingOptions.Section);
builder.Services.AddRateLimiter(limiter =>
{
    limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    limiter.AddFixedWindowLimiter(RateLimitingOptions.SubmissionsPolicy, window =>
    {
        window.PermitLimit = limits.SubmissionsPerMinute;
        window.Window = TimeSpan.FromMinutes(1);
    });
});

// The largest valid order (100 lines with 200-character titles) is far below this; anything bigger is refused unread.
builder.WebHost.ConfigureKestrel(kestrel => kestrel.Limits.MaxRequestBodySize = 256 * 1024);

builder.Services.AddHealthChecks().AddCheck<ReadinessCheck>("sql");
builder.Services.AddApiDocs();

var app = builder.Build();

// Migrating at startup needs the app's identity to be the SQL admin (ADR 7).
await using (var scope = app.Services.CreateAsyncScope())
    await scope.ServiceProvider.GetRequiredService<ShopDbContext>().Database.MigrateAsync();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRateLimiter();
app.MapControllers();

// Liveness only asks whether the process answers; readiness also needs SQL Server.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready");

// The docs are public in every environment: the deployed demo is there to be tried.
app.MapOpenApi();
app.MapScalarApiReference();
app.MapGet("/", () => TypedResults.Redirect("/scalar/")).ExcludeFromDescription();

await app.RunAsync();
