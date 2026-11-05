using System.Globalization;

namespace FunBooksAndVideos.Domain.Tests;

public sealed class ValueObjectTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("48.50")]
    [InlineData("48.500")]
    public void Money_accepts_whole_pence(string amount) =>
        Assert.Equal(Parse(amount), new Money(Parse(amount)).Amount);

    [Theory]
    [InlineData("-0.01")]
    [InlineData("48.505")]
    public void Money_rejects_negative_amounts_and_fractions_of_a_penny(string amount) =>
        Assert.Throws<DomainException>(() => new Money(Parse(amount)));

    [Fact]
    public void Money_compares_by_value() => Assert.Equal(new Money(48.5m), new Money(48.50m));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void An_id_is_required(string value)
    {
        Assert.Throws<DomainException>(() => new PurchaseOrderId(value));
        Assert.Throws<DomainException>(() => new CustomerId(value));
    }

    [Fact]
    public void An_id_has_a_maximum_length() =>
        Assert.Throws<DomainException>(() => new CustomerId(new string('9', Identifier.MaxLength + 1)));

    [Fact]
    public void Ids_compare_by_value() => Assert.Equal(new CustomerId("4567890"), new CustomerId("4567890"));

    [Fact]
    public void Premium_is_both_clubs() =>
        Assert.Equal(MembershipType.BookClub | MembershipType.VideoClub, MembershipType.Premium);

    private static decimal Parse(string amount) => decimal.Parse(amount, CultureInfo.InvariantCulture);
}
