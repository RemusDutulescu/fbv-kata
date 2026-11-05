using FunBooksAndVideos.Api.Persistence;
using FunBooksAndVideos.Api.Processing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Temporalio.Client;
using Temporalio.Testing;
using Testcontainers.MsSql;

[assembly: AssemblyFixture(typeof(FunBooksAndVideos.Api.Tests.AppFixture))]

namespace FunBooksAndVideos.Api.Tests;

/// <summary>
/// The whole application, once per test run: a real SQL Server (the production engine), a real Temporal dev server,
/// and the API with its hosted worker composed exactly as Program.cs composes it.
/// </summary>
public sealed class AppFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
    private WorkflowEnvironment? _temporal;
    private WebApplicationFactory<Program>? _app;

    public ITemporalClient Temporal => _temporal?.Client ?? throw NotStarted();

    public WebApplicationFactory<Program> App => _app ?? throw NotStarted();

    public async ValueTask InitializeAsync()
    {
        var temporal = WorkflowEnvironment.StartLocalAsync(new()
        {
            DataConverter = ProcessingServiceCollectionExtensions.TemporalDataConverter,
        });
        await Task.WhenAll(_sql.StartAsync(), temporal);
        _temporal = await temporal;

        // UseSetting, not ConfigureAppConfiguration: Program.cs reads its settings while it registers services, before
        // configuration added that way is in place.
        _app = new WebApplicationFactory<Program>().WithWebHostBuilder(web => web
            .UseSetting("ConnectionStrings:Shop", _sql.GetConnectionString())
            .UseSetting("Temporal:Address", _temporal.Client.Connection.Options.TargetHost)
            .UseSetting("RateLimiting:SubmissionsPerMinute", "1000"));

        // Starting the host migrates the database and starts the Temporal worker.
        _ = _app.Server;
    }

    public ShopDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<ShopDbContext>().UseSqlServer(_sql.GetConnectionString()).Options);

    public async ValueTask DisposeAsync()
    {
        if (_app is not null) await _app.DisposeAsync();
        if (_temporal is not null) await _temporal.ShutdownAsync();
        await _sql.DisposeAsync();
    }

    private static InvalidOperationException NotStarted() => new("The test fixture has not started.");
}
