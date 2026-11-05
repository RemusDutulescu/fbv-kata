using System.ComponentModel.DataAnnotations;

namespace FunBooksAndVideos.Api.Http;

public sealed class RateLimitingOptions
{
    public const string Section = "RateLimiting";

    /// <summary>The rate limiter policy the submission action names.</summary>
    public const string SubmissionsPolicy = "submissions";

    /// <summary>Submissions accepted per minute, across all clients together.</summary>
    [Range(1, int.MaxValue)]
    public required int SubmissionsPerMinute { get; init; }
}
