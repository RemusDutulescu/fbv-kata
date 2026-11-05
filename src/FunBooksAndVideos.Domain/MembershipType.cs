namespace FunBooksAndVideos.Domain;

/// <summary>
/// Club memberships as flags. Premium is literally both clubs, so a video-club member who buys
/// the book club ends up Premium, and activating a membership is a set union: idempotent by construction.
/// </summary>
[Flags]
public enum MembershipType
{
    None = 0,
    BookClub = 1,
    VideoClub = 2,
    Premium = BookClub | VideoClub,
}

public static class MembershipTypeExtensions
{
    /// <summary>True for BookClub, VideoClub or Premium; false for None or undefined bits.</summary>
    public static bool IsValid(this MembershipType membership) =>
        membership != MembershipType.None && (membership & ~MembershipType.Premium) == 0;
}
