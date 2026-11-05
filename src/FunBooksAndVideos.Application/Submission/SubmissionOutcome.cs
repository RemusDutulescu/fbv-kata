namespace FunBooksAndVideos.Application.Submission;

/// <summary>Result pattern: every expected outcome of a submission is a value rather than an exception, with one HTTP status each.</summary>
public enum SubmissionOutcome
{
    /// <summary>Accepted now, and its Immediate commands have run.</summary>
    Created,

    /// <summary>A retry of an order already stored, and its Immediate commands have run.</summary>
    Replayed,

    /// <summary>Stored, but its Immediate commands weren't confirmed in time; it carries on and can be polled.</summary>
    Pending,

    /// <summary>The id is already taken: by a different order, or by an earlier submission that can't run again.</summary>
    Conflict,

    /// <summary>Not confirmed in time and not stored yet, so nothing is known about it; resubmitting is safe.</summary>
    Unconfirmed,
}
