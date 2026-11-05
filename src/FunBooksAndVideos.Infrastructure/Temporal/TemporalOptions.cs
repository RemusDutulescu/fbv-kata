using System.ComponentModel.DataAnnotations;

namespace FunBooksAndVideos.Infrastructure.Temporal;

/// <summary>Where Temporal is, and the task queue on which the API starts workflows and the workers pick them up.</summary>
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
}
