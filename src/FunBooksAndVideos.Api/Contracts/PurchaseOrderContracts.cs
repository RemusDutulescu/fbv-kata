using System.Text.Json.Serialization;

namespace FunBooksAndVideos.Api.Contracts;

// The public HTTP contract, versioned under /v1 and kept apart from the domain (an anti-corruption layer). Every field
// is required and only ShippingSlip may be null, which the serializer enforces (HttpApi.StrictJson) and the OpenAPI
// document states.

/// <summary>A purchase order to process.</summary>
/// <param name="Id">The order id from the shop. It is also the idempotency key: resubmitting it is always safe.</param>
/// <param name="CustomerId">The customer account the order belongs to.</param>
/// <param name="Total">The order total, in GBP.</param>
/// <param name="Items">One line per product or membership.</param>
public sealed record SubmitPurchaseOrderRequest(string Id, string CustomerId, decimal Total, IReadOnlyList<ItemLineDto> Items);

/// <summary>An item line: a book, a video or a membership, told apart by <c>type</c>.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(BookItemDto), "book")]
[JsonDerivedType(typeof(VideoItemDto), "video")]
[JsonDerivedType(typeof(MembershipItemDto), "membership")]
public abstract record ItemLineDto;

/// <summary>A book. Books are shipped.</summary>
public sealed record BookItemDto(string Title) : ItemLineDto;

/// <summary>A video. Videos are watched online.</summary>
public sealed record VideoItemDto(string Title) : ItemLineDto;

/// <summary>A membership to activate.</summary>
public sealed record MembershipItemDto(MembershipDto Membership) : ItemLineDto;

/// <summary>A club membership. Premium is both clubs.</summary>
public enum MembershipDto
{
    // The domain's flag values. The serializer also accepts "bookClub, videoClub", which then means both clubs (Premium)
    // instead of silently landing on whichever club has the right number.
    BookClub = 1,
    VideoClub = 2,
    Premium = 3,
}

/// <summary>Where an order's fulfilment stands.</summary>
public enum OrderStatusDto
{
    Processing,
    Completed,
}

/// <summary>A purchase order and where its fulfilment stands.</summary>
/// <param name="ShippingSlip">The shipping slip, once generated: null until then, and for orders with nothing to ship.</param>
public sealed record PurchaseOrderResponse(
    string Id,
    string CustomerId,
    decimal Total,
    IReadOnlyList<ItemLineDto> Items,
    OrderStatusDto Status,
    DateTimeOffset AcceptedAt,
    ShippingSlipResponse? ShippingSlip);

/// <summary>A shipping slip (BR2): the physical products of an order.</summary>
public sealed record ShippingSlipResponse(IReadOnlyList<ItemLineDto> Items, DateTimeOffset GeneratedAt);

/// <summary>The clubs a customer belongs to.</summary>
/// <param name="Premium">True when the customer belongs to both clubs.</param>
public sealed record MembershipsResponse(string CustomerId, IReadOnlyList<MembershipDto> Clubs, bool Premium);
