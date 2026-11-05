using System.Diagnostics;
using FunBooksAndVideos.Api.Persistence;
using FunBooksAndVideos.Domain;

namespace FunBooksAndVideos.Api.Contracts;

/// <summary>Translation between the public contract and the domain, by hand.</summary>
public static class ContractMapping
{
    private static readonly MembershipType[] Clubs = [MembershipType.BookClub, MembershipType.VideoClub];

    public static PurchaseOrder ToDomain(this SubmitPurchaseOrderRequest request) => new(
        new PurchaseOrderId(request.Id),
        new CustomerId(request.CustomerId),
        new Money(request.Total),
        request.Items.Select(ToDomain));

    public static PurchaseOrderResponse ToResponse(this PurchaseOrderRecord order, ShippingSlipRecord? slip) => new(
        order.Id.Value,
        order.CustomerId.Value,
        order.Total.Amount,
        [.. order.Lines.Select(ToContract)],
        order.Status switch
        {
            PurchaseOrderStatus.Processing => OrderStatusDto.Processing,
            PurchaseOrderStatus.Completed => OrderStatusDto.Completed,
            _ => throw new UnreachableException($"No contract for status {order.Status}."),
        },
        order.AcceptedAt,
        slip is null ? null : new ShippingSlipResponse([.. slip.Items.Select(ToContract)], slip.GeneratedAt));

    public static MembershipsResponse ToResponse(this MembershipType memberships, CustomerId customerId) => new(
        customerId.Value,
        [.. Clubs.Where(club => memberships.HasFlag(club)).Select(ToContract)],
        memberships == MembershipType.Premium);

    private static ItemLine ToDomain(ItemLineDto item) => item switch
    {
        BookItemDto book => new BookLine(book.Title),
        VideoItemDto video => new VideoLine(video.Title),
        MembershipItemDto membership => new MembershipLine(ToDomain(membership.Membership)),
        _ => throw new DomainException("An item line has no recognised type."),
    };

    private static MembershipType ToDomain(MembershipDto membership) => membership switch
    {
        MembershipDto.BookClub => MembershipType.BookClub,
        MembershipDto.VideoClub => MembershipType.VideoClub,
        MembershipDto.Premium => MembershipType.Premium,
        _ => throw new DomainException($"'{membership}' is not a membership."),
    };

    private static ItemLineDto ToContract(ItemLine line) => line switch
    {
        BookLine book => new BookItemDto(book.Title),
        VideoLine video => new VideoItemDto(video.Title),
        MembershipLine membership => new MembershipItemDto(ToContract(membership.Membership)),
        _ => throw new UnreachableException($"No contract for {line.GetType().Name}."),
    };

    private static MembershipDto ToContract(MembershipType membership) => membership switch
    {
        MembershipType.BookClub => MembershipDto.BookClub,
        MembershipType.VideoClub => MembershipDto.VideoClub,
        MembershipType.Premium => MembershipDto.Premium,
        _ => throw new UnreachableException($"No contract for membership {membership}."),
    };
}
