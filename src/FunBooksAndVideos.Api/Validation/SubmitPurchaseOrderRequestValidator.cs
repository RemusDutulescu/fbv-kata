using FluentValidation;
using FunBooksAndVideos.Api.Contracts;
using FunBooksAndVideos.Domain;

namespace FunBooksAndVideos.Api.Validation;

/// <summary>
/// Contract validation: the values that the JSON's shape can't express (ADR 6). It reports every problem at once,
/// keyed by JSON path, so a client can fix them in one go.
/// </summary>
public sealed class SubmitPurchaseOrderRequestValidator : AbstractValidator<SubmitPurchaseOrderRequest>
{
    // Ids end up in URLs, headers and workflow ids, so they are limited to characters that never need escaping.
    // \z rather than $: in .NET, $ also matches before a final newline, and a newline in a Location header is a 500.
    private const string IdPattern = @"^[A-Za-z0-9_-]+\z";
    private const string IdMessage = "'{PropertyName}' may contain only letters, digits, '-' and '_'.";

    public SubmitPurchaseOrderRequestValidator()
    {
        RuleFor(r => r.Id).NotEmpty().MaximumLength(Identifier.MaxLength).Matches(IdPattern).WithMessage(IdMessage);
        RuleFor(r => r.CustomerId).NotEmpty().MaximumLength(Identifier.MaxLength).Matches(IdPattern).WithMessage(IdMessage);
        RuleFor(r => r.Total).GreaterThanOrEqualTo(0).PrecisionScale(18, 2, ignoreTrailingZeros: true);

        RuleFor(r => r.Items)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(items => items.Count <= PurchaseOrder.MaxLines)
            .WithMessage($"An order has at most {PurchaseOrder.MaxLines} item lines.");

        RuleForEach(r => r.Items).NotNull().SetInheritanceValidator(lines =>
        {
            lines.Add(new BookItemValidator());
            lines.Add(new VideoItemValidator());
            lines.Add(new MembershipItemValidator());
        });
    }

    private sealed class BookItemValidator : AbstractValidator<BookItemDto>
    {
        public BookItemValidator() => RuleFor(book => book.Title).NotEmpty().MaximumLength(ProductLine.MaxTitleLength);
    }

    private sealed class VideoItemValidator : AbstractValidator<VideoItemDto>
    {
        public VideoItemValidator() => RuleFor(video => video.Title).NotEmpty().MaximumLength(ProductLine.MaxTitleLength);
    }

    private sealed class MembershipItemValidator : AbstractValidator<MembershipItemDto>
    {
        public MembershipItemValidator() => RuleFor(item => item.Membership).IsInEnum();
    }
}
