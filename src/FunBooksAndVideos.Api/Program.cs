using FunBooksAndVideos.Api.Endpoints;
using FunBooksAndVideos.Application.Queries;
using FunBooksAndVideos.Application.Submission;
using FunBooksAndVideos.Infrastructure;
using FunBooksAndVideos.Infrastructure.Observability;
using FunBooksAndVideos.Infrastructure.Persistence;
using FunBooksAndVideos.Infrastructure.Temporal;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddShopDatabase(builder.Configuration);
builder.Services.AddTemporal(builder.Configuration);
builder.Services.AddTelemetry(builder.Configuration, "FunBooksAndVideos.Api")
    // Probes call every few seconds; their traces would be noise, and they'd cost ingestion.
    .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation(http => http.Filter = context => !context.Request.Path.StartsWithSegments("/health")));

// Submissions go to Temporal, where the worker picks them up; reads come straight from SQL.
builder.Services.AddRequiredOptions<SubmissionOptions>(builder.Configuration, SubmissionOptions.Section);
builder.Services.AddScoped<OrderSubmitter>();
builder.Services.AddScoped<ShopQueries>();

builder.Services.AddHttpApi(builder.Configuration);

// The largest valid order (100 lines with 200-character titles) is far below this; anything bigger is refused unread.
builder.WebHost.ConfigureKestrel(kestrel => kestrel.Limits.MaxRequestBodySize = 256 * 1024);

var app = builder.Build();

// Migrating at startup needs the app's identity to be the SQL admin; a migration bundle run by the pipeline would
// leave it data rights only.
await using (var scope = app.Services.CreateAsyncScope())
    await scope.ServiceProvider.GetRequiredService<ShopDbContext>().Database.MigrateAsync();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRateLimiter();
app.MapHttpApi();

await app.RunAsync();
