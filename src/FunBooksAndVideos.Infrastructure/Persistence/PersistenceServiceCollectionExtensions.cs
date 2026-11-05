using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FunBooksAndVideos.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    /// <summary>The shop's SQL Server database, for both hosts: the API reads it, and the worker's activities write it.</summary>
    public static IServiceCollection AddShopDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionStrings = services.AddRequiredOptions<ConnectionStrings>(configuration, ConnectionStrings.Section);
        return services.AddDbContext<ShopDbContext>(options => options.UseSqlServer(
            connectionStrings.Shop,
            sql => sql.EnableRetryOnFailure()));
    }
}
