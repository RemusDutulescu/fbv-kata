using FunBooksAndVideos.Infrastructure.Persistence;
using FunBooksAndVideos.Infrastructure.Temporal;
using FunBooksAndVideos.Worker;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Temporalio.Client;
using Temporalio.Testing;
using Testcontainers.MsSql;

[assembly: AssemblyFixture(typeof(FunBooksAndVideos.IntegrationTests.AppFixture))]

namespace FunBooksAndVideos.IntegrationTests;

/// <summary>
/// The whole application, once per test run: a real SQL Server (the production engine), a real Temporal dev server,
/// the API composed exactly as its Program.cs composes it, and a worker composed as the worker's.
/// </summary>
public sealed class AppFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
    private WorkflowEnvironment? _temporal;
    private WebApplicationFactory<Program>? _app;
    private IHost? _worker;

    public ITemporalClient Temporal => _temporal?.Client ?? throw NotStarted();

    public WebApplicationFactory<Program> App => _app ?? throw NotStarted();

    public async ValueTask InitializeAsync()
    {
        var temporal = WorkflowEnvironment.StartLocalAsync(new()
        {
            DataConverter = TemporalServiceCollectionExtensions.TemporalDataConverter,
        });
        await Task.WhenAll(_sql.StartAsync(), temporal);
        _temporal = await temporal;

        // UseSetting, not ConfigureAppConfiguration: Program.cs reads its settings while it registers services, before
        // configuration added that way is in place.
        _app = new WebApplicationFactory<Program>().WithWebHostBuilder(web => web
            .UseSetting("ConnectionStrings:Shop", _sql.GetConnectionString())
            .UseSetting("Temporal:Address", _temporal.Client.Connection.Options.TargetHost)
            .UseSetting("RateLimiting:SubmissionsPerMinute", "1000"));

        // Starting the API migrates the database, before the worker's activities use it.
        _ = _app.Server;

        // The worker reads its own appsettings.json, so the API and the worker have to agree on the task queue.
        var worker = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { ContentRootPath = WorkerProjectFolder() });
        worker.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Shop"] = _sql.GetConnectionString(),
            ["Temporal:Address"] = _temporal.Client.Connection.Options.TargetHost,
            ["Temporal:Namespace"] = _temporal.Client.Options.Namespace,
        });
        worker.Services.AddPurchaseOrderWorker(worker.Configuration);
        _worker = worker.Build();
        await _worker.StartAsync();
    }

    /// <summary>The API on a task queue no worker polls, so no submission is ever acknowledged, and it gives up after a second.</summary>
    public WebApplicationFactory<Program> ApiWithoutWorkers() => App.WithWebHostBuilder(web => web
        .UseSetting("Temporal:TaskQueue", "no-workers")
        .UseSetting("Submission:AcknowledgementTimeout", "00:00:01"));

    public ShopDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<ShopDbContext>().UseSqlServer(_sql.GetConnectionString()).Options);

    public async ValueTask DisposeAsync()
    {
        if (_worker is not null)
        {
            await _worker.StopAsync();
            _worker.Dispose();
        }

        if (_app is not null) await _app.DisposeAsync();
        if (_temporal is not null) await _temporal.ShutdownAsync();
        await _sql.DisposeAsync();
    }

    /// <summary>The worker's source folder, as WebApplicationFactory finds the API's: from the solution's.</summary>
    private static string WorkerProjectFolder()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);
        while (folder is not null && !File.Exists(Path.Combine(folder.FullName, "FunBooksAndVideos.slnx")))
            folder = folder.Parent;

        return Path.Combine(folder?.FullName ?? throw new DirectoryNotFoundException("The solution folder was not found."), "src", "FunBooksAndVideos.Worker");
    }

    private static InvalidOperationException NotStarted() => new("The test fixture has not started.");
}
