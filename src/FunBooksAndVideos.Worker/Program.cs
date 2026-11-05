using FunBooksAndVideos.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddPurchaseOrderWorker(builder.Configuration);
await builder.Build().RunAsync();
