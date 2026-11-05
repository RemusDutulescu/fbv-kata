using FunBooksAndVideos.Api.Endpoints;
using FunBooksAndVideos.Api.Persistence;
using FunBooksAndVideos.Api.Processing;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ShopDbContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("Shop"),
    sql => sql.EnableRetryOnFailure()));
builder.Services.AddOrderProcessing(builder.Configuration);
builder.Services.AddHttpApi(builder.Configuration);

// The largest valid order (100 lines with 200-character titles) is far below this; anything bigger is refused unread.
builder.WebHost.ConfigureKestrel(kestrel => kestrel.Limits.MaxRequestBodySize = 256 * 1024);

var app = builder.Build();

// ponytail: migrations at startup suit one replica; with several, run an EF migration bundle in the pipeline instead.
await using (var scope = app.Services.CreateAsyncScope())
    await scope.ServiceProvider.GetRequiredService<ShopDbContext>().Database.MigrateAsync();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRateLimiter();
app.MapHttpApi();

await app.RunAsync();
