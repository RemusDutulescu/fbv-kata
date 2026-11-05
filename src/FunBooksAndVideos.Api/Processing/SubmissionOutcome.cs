using FunBooksAndVideos.Domain;

namespace FunBooksAndVideos.Api.Processing;

/// <summary>
/// Result pattern: every expected outcome of a submission as a closed set (the base constructor is private).
/// Expected outcomes are values rather than exceptions, and each one maps to exactly one HTTP status.
/// </summary>
public abstract record SubmissionOutcome
{
    private SubmissionOutcome(PurchaseOrderId id) => Id = id;

    public PurchaseOrderId Id { get; }

    /// <summary>Accepted now, and its Immediate commands have run.</summary>
    public sealed record Created(PurchaseOrderId Id) : SubmissionOutcome(Id);

    /// <summary>The same order was submitted before: a client retry.</summary>
    public sealed record Replayed(PurchaseOrderId Id) : SubmissionOutcome(Id);

    /// <summary>Accepted, but acknowledging it took longer than promised; it carries on and can be polled.</summary>
    public sealed record Pending(PurchaseOrderId Id) : SubmissionOutcome(Id);

    /// <summary>The id already belongs to a different order.</summary>
    public sealed record Conflict(PurchaseOrderId Id) : SubmissionOutcome(Id);
}
