using System.ComponentModel.DataAnnotations;

namespace FunBooksAndVideos.Api.Endpoints;

public sealed class RateLimitingOptions
{
    public const string Section = "RateLimiting";

    /// <summary>Submissions accepted per minute, across all clients together.</summary>
    [Range(1, int.MaxValue)]
    public required int SubmissionsPerMinute { get; init; }
}
