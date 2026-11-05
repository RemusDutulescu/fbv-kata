using System.Text.Json;
using FunBooksAndVideos.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FunBooksAndVideos.Api.Persistence;

/// <summary>
/// The unit of work. Each Temporal activity uses one context and one SaveChanges, so every step commits atomically.
/// The domain has no EF attributes: all mapping lives here.
/// </summary>
public sealed class ShopDbContext(DbContextOptions<ShopDbContext> options) : DbContext(options)
{
    // SQL Server compares text case-insensitively by default, but Temporal's workflow ids are case-sensitive:
    // "ABC" and "abc" must be two orders in both, or two workflows would share one row.
    private const string CaseSensitive = "Latin1_General_100_BIN2";

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<PurchaseOrderRecord> PurchaseOrders => Set<PurchaseOrderRecord>();

    public DbSet<ShippingSlipRecord> ShippingSlips => Set<ShippingSlipRecord>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<PurchaseOrderId>()
            .HaveConversion<PurchaseOrderIdConverter>().HaveMaxLength(Identifier.MaxLength).UseCollation(CaseSensitive);
        configurationBuilder.Properties<CustomerId>()
            .HaveConversion<CustomerIdConverter>().HaveMaxLength(Identifier.MaxLength).UseCollation(CaseSensitive);
        configurationBuilder.Properties<Money>().HaveConversion<MoneyConverter>().HavePrecision(18, 2);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(customer =>
        {
            customer.ToTable("Customers");
            customer.HasKey(c => c.Id);

            // Optimistic concurrency, invisible to the domain: if two orders change the same customer at once,
            // the second SaveChanges throws and its Temporal activity is retried against fresh data.
            customer.Property<byte[]>("RowVersion").IsRowVersion();
        });

        modelBuilder.Entity<PurchaseOrderRecord>(order =>
        {
            order.ToTable("PurchaseOrders");
            order.HasKey(o => o.Id);
            order.Property(o => o.Lines).HasJsonConversion();
            order.Property(o => o.Status).HasConversion<string>().HasMaxLength(16).IsUnicode(false);
        });

        modelBuilder.Entity<ShippingSlipRecord>(slip =>
        {
            slip.ToTable("ShippingSlips");
            slip.HasKey(s => s.PurchaseOrderId);
            slip.Property(s => s.Items).HasJsonConversion();
            slip.HasOne<PurchaseOrderRecord>().WithOne().HasForeignKey<ShippingSlipRecord>(s => s.PurchaseOrderId);
        });
    }

    private sealed class PurchaseOrderIdConverter()
        : ValueConverter<PurchaseOrderId, string>(id => id.Value, value => new PurchaseOrderId(value));

    private sealed class CustomerIdConverter()
        : ValueConverter<CustomerId, string>(id => id.Value, value => new CustomerId(value));

    private sealed class MoneyConverter()
        : ValueConverter<Money, decimal>(money => money.Amount, amount => new Money(amount));
}

internal static class JsonColumn
{
    /// <summary>Stores a list of domain values as a JSON column, compared by value for change tracking.</summary>
    public static PropertyBuilder<IReadOnlyList<T>> HasJsonConversion<T>(this PropertyBuilder<IReadOnlyList<T>> property) =>
        property.HasConversion(
            list => JsonSerializer.Serialize(list, DomainJson.Options),
            json => JsonSerializer.Deserialize<IReadOnlyList<T>>(json, DomainJson.Options) ?? Array.Empty<T>(),
            new ValueComparer<IReadOnlyList<T>>(
                (left, right) => left!.SequenceEqual(right!),
                list => list.Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
                list => list.ToArray()));
}
