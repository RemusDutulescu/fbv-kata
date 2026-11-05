using System.ComponentModel.DataAnnotations;

namespace FunBooksAndVideos.Api.Processing;

/// <summary>Where Temporal is and how the API uses it. Invalid values stop the app at startup, not at the first order.</summary>
public sealed class TemporalOptions
{
    public const string Section = "Temporal";

    [Required]
    public string Address { get; set; } = "localhost:7233";

    [Required]
    public string Namespace { get; set; } = "default";

    [Required]
    public string TaskQueue { get; set; } = "purchase-orders";

    /// <summary>Temporal Cloud API key. When set, the connection uses TLS. Supplied as a secret, never committed.</summary>
    public string? ApiKey { get; set; }

    /// <summary>How long a caller waits for BR1's immediate work before getting 202 and a link to poll.</summary>
    [Range(typeof(TimeSpan), "00:00:01", "00:01:00")]
    public TimeSpan AcknowledgementTimeout { get; set; } = TimeSpan.FromSeconds(10);
}
