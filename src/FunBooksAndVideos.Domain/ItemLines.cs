namespace FunBooksAndVideos.Domain;

// The whole item-line hierarchy lives in one file so it can be read at a glance.

/// <summary>One line of a purchase order: a product or a membership request.</summary>
public abstract record ItemLine;

/// <summary>
/// A product. Whether it ships is a property of the product, so the shipping rule asks
/// <see cref="IsPhysical"/> instead of checking for books: an e-book or a DVD is a new subtype, not a rule change.
/// </summary>
public abstract record ProductLine : ItemLine
{
    public const int MaxTitleLength = 200;

    protected ProductLine(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("A product needs a title.");
        if (title.Length > MaxTitleLength) throw new DomainException($"A product title is at most {MaxTitleLength} characters.");

        Title = title;
    }

    public string Title { get; }

    public abstract bool IsPhysical { get; }
}

/// <summary>A book. Books are shipped to the customer.</summary>
public sealed record BookLine(string Title) : ProductLine(Title)
{
    public override bool IsPhysical => true;
}

/// <summary>A video. Videos are watched online, so there is nothing to ship.</summary>
public sealed record VideoLine(string Title) : ProductLine(Title)
{
    public override bool IsPhysical => false;
}

/// <summary>A request to activate a membership in the customer's account.</summary>
public sealed record MembershipLine : ItemLine
{
    public MembershipLine(MembershipType membership)
    {
        if (!membership.IsValid()) throw new DomainException($"'{membership}' is not a membership.");

        Membership = membership;
    }

    public MembershipType Membership { get; }
}
