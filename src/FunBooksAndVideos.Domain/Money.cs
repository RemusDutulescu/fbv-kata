using System.Globalization;

namespace FunBooksAndVideos.Domain;

/// <summary>
/// An amount of money: never negative, never a fraction of a penny. One currency for now (the shop
/// prices in GBP); supporting a second one would turn <see cref="Currency"/> into a field.
/// A struct, because its default (0.00) is a valid amount.
/// </summary>
public readonly record struct Money
{
    public const string Currency = "GBP";

    public Money(decimal amount)
    {
        if (amount < 0) throw new DomainException("An amount can't be negative.");
        if (decimal.Round(amount, 2) != amount) throw new DomainException("An amount has at most two decimal places.");

        Amount = amount;
    }

    public decimal Amount { get; }

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Amount:0.00} {Currency}");
}
