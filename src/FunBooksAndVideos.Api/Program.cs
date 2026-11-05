using FunBooksAndVideos.Api.Persistence;
using FunBooksAndVideos.Api.Processing;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ShopDbContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("Shop"),
    sql => sql.EnableRetryOnFailure()));
builder.Services.AddOrderProcessing(builder.Configuration);

var app = builder.Build();

// ponytail: migrations at startup suit one replica; with several, run an EF migration bundle in the pipeline instead.
await using (var scope = app.Services.CreateAsyncScope())
    await scope.ServiceProvider.GetRequiredService<ShopDbContext>().Database.MigrateAsync();

await app.RunAsync();
