using System.ComponentModel.DataAnnotations;

namespace FunBooksAndVideos.Application.Submission;

public sealed class SubmissionOptions
{
    public const string Section = "Submission";

    /// <summary>How long a submission waits for BR1's immediate work before answering 202, or 503 if the order isn't stored yet.</summary>
    [Range(typeof(TimeSpan), "00:00:01", "00:01:00")]
    public required TimeSpan AcknowledgementTimeout { get; init; }
}
