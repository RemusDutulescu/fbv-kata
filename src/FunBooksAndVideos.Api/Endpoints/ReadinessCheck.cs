using FunBooksAndVideos.Api.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Temporalio.Client;

namespace FunBooksAndVideos.Api.Endpoints;

/// <summary>Ready means both dependencies answer: without either one, a submission can't be processed.</summary>
internal sealed class ReadinessCheck(ShopDbContext db, ITemporalClient temporal) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!await db.Database.CanConnectAsync(cancellationToken))
            return HealthCheckResult.Unhealthy("SQL Server is unreachable.");

        return await temporal.Connection.CheckHealthAsync(options: new() { CancellationToken = cancellationToken })
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Temporal is unreachable.");
    }
}
