using System.ComponentModel.DataAnnotations;

namespace FunBooksAndVideos.Api.Persistence;

public sealed class ConnectionStrings
{
    public const string Section = "ConnectionStrings";

    /// <summary>The SQL Server database behind <see cref="ShopDbContext"/>.</summary>
    [Required]
    public required string Shop { get; init; }
}
