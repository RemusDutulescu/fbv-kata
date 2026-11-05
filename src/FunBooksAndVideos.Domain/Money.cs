namespace FunBooksAndVideos.Domain;

/// <summary>
/// An amount of money in GBP: never negative, never a fraction of a penny.
/// A struct, because its default (0.00) is a valid amount.
/// </summary>
public readonly record struct Money
{
    public Money(decimal amount)
    {
        if (amount < 0) throw new DomainException("An amount can't be negative.");
        if (decimal.Round(amount, 2) != amount) throw new DomainException("An amount has at most two decimal places.");

        Amount = amount;
    }

    public decimal Amount { get; }
}
