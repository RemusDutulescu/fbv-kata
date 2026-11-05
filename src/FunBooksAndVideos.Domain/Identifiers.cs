namespace FunBooksAndVideos.Domain;

// Strongly-typed ids: both are plain text on the wire, so as bare strings they could be swapped
// without the compiler noticing. They are classes, not structs, because an id has no valid default.

/// <summary>Identifies a purchase order. Opaque text: the upstream order system assigns it.</summary>
public sealed record PurchaseOrderId
{
    public PurchaseOrderId(string value) => Value = Identifier.Require(value, "A purchase order id");

    public string Value { get; }

    public override string ToString() => Value;
}

/// <summary>Identifies a customer account. Opaque text: the upstream customer system assigns it.</summary>
public sealed record CustomerId
{
    public CustomerId(string value) => Value = Identifier.Require(value, "A customer id");

    public string Value { get; }

    public override string ToString() => Value;
}

public static class Identifier
{
    public const int MaxLength = 64;

    internal static string Require(string value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new DomainException($"{name} is required.")
        : value.Length > MaxLength ? throw new DomainException($"{name} is at most {MaxLength} characters.")
        : value;
}
