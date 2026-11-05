using FunBooksAndVideos.Api;
using FunBooksAndVideos.Api.Endpoints;
using FunBooksAndVideos.Api.Observability;
using FunBooksAndVideos.Api.Persistence;
using FunBooksAndVideos.Api.Processing;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionStrings = builder.Services.AddRequiredOptions<ConnectionStrings>(builder.Configuration, ConnectionStrings.Section);
builder.Services.AddDbContext<ShopDbContext>(options => options.UseSqlServer(
    connectionStrings.Shop,
    sql => sql.EnableRetryOnFailure()));
builder.Services.AddOrderProcessing(builder.Configuration);
builder.Services.AddHttpApi(builder.Configuration);
builder.Services.AddTelemetry(builder.Configuration);

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
