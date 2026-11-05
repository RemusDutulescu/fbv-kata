namespace FunBooksAndVideos.Api.Processing;

/// <summary>Result pattern: every expected outcome of a submission is a value rather than an exception, with one HTTP status each.</summary>
public enum SubmissionOutcome
{
    /// <summary>Accepted now, and its Immediate commands have run.</summary>
    Created,

    /// <summary>The same order was submitted before: a client retry.</summary>
    Replayed,

    /// <summary>Accepted, but acknowledging it took longer than promised; it carries on and can be polled.</summary>
    Pending,

    /// <summary>The id already belongs to a different order.</summary>
    Conflict,
}
