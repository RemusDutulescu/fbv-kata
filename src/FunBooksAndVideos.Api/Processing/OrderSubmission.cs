using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using FunBooksAndVideos.Domain;

namespace FunBooksAndVideos.Api.Processing;

/// <summary>
/// The workflow's input: a validated order as plain data. It is its own type, separate from the HTTP contract,
/// so the API can change shape without breaking the replay of workflows already running.
/// </summary>
public sealed record OrderSubmission(
    PurchaseOrderId Id,
    CustomerId CustomerId,
    decimal Total,
    IReadOnlyList<ItemLine> Lines,
    string PayloadHash)
{
    public static OrderSubmission From(PurchaseOrder order) =>
        new(order.Id, order.CustomerId, order.Total.Amount, order.Lines, Fingerprint(order));

    public PurchaseOrder ToPurchaseOrder() => new(Id, CustomerId, new Money(Total), Lines);

    /// <summary>
    /// SHA-256 of the order's canonical JSON. The same order always hashes the same, whatever its JSON formatting
    /// or the trailing zeros of its total, so a retry is never mistaken for a conflicting reuse of the id.
    /// </summary>
    private static string Fingerprint(PurchaseOrder order)
    {
        var canonical = JsonSerializer.SerializeToUtf8Bytes(
            new
            {
                order.Id,
                order.CustomerId,
                Total = order.Total.Amount.ToString("0.00", CultureInfo.InvariantCulture),
                order.Lines,
            },
            DomainJson.Options);

        return Convert.ToHexStringLower(SHA256.HashData(canonical));
    }
}
