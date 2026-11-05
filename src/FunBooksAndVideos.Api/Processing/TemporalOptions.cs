using System.ComponentModel.DataAnnotations;

namespace FunBooksAndVideos.Api.Processing;

/// <summary>Where Temporal is and how the API uses it.</summary>
public sealed class TemporalOptions
{
    public const string Section = "Temporal";

    [Required]
    public required string Address { get; init; }

    [Required]
    public required string Namespace { get; init; }

    [Required]
    public required string TaskQueue { get; init; }

    /// <summary>Temporal Cloud API key. When set, the connection uses TLS. Supplied as a secret, never committed.</summary>
    public string? ApiKey { get; init; }

    /// <summary>How long a submission waits for BR1's immediate work before answering 202, or 503 if the order isn't stored yet.</summary>
    [Range(typeof(TimeSpan), "00:00:01", "00:01:00")]
    public required TimeSpan AcknowledgementTimeout { get; init; }
}
