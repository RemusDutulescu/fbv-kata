using FunBooksAndVideos.Api.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FunBooksAndVideos.Api.Endpoints;

/// <summary>
/// Ready means SQL Server answers: nothing works without it. Temporal is left out, so a blip there doesn't take the
/// read endpoints offline with it.
/// </summary>
internal sealed class ReadinessCheck(ShopDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        await db.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("SQL Server is unreachable.");
}
