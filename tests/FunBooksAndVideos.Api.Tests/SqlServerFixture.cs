using FunBooksAndVideos.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

[assembly: AssemblyFixture(typeof(FunBooksAndVideos.Api.Tests.SqlServerFixture))]

namespace FunBooksAndVideos.Api.Tests;

/// <summary>One real SQL Server for the whole test run, migrated once: tests run on the same engine as production.</summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    public ShopDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<ShopDbContext>().UseSqlServer(ConnectionString).Options);

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
