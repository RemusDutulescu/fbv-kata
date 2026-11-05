namespace FunBooksAndVideos.Application.Submission;

/// <summary>Result pattern: every expected outcome of a submission is a value rather than an exception, with one HTTP status each.</summary>
public enum SubmissionOutcome
{
    /// <summary>Accepted now, and its Immediate commands have run.</summary>
    Created,

    /// <summary>The same order was submitted before, a client retry, and its Immediate commands have run.</summary>
    Replayed,

    /// <summary>Stored, but its Immediate commands are taking longer than promised; it carries on and can be polled.</summary>
    Pending,

    /// <summary>The id already belongs to a different order.</summary>
    Conflict,

    /// <summary>Not confirmed in time and not stored yet, so nothing is known about it; resubmitting is safe.</summary>
    Unconfirmed,
}
