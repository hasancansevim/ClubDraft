using ClubCraft.BuildingBlocks.Common.SeedWork;

namespace ClubCraft.ClubManagement.Domain.ValueObjects;

public class Money : ValueObject
{
    public decimal Amount { get; }

    private Money() { } // EF Core

    public Money(decimal amount)
    {
        if (amount < 0)
            throw new ArgumentException("Money amount cannot be negative.", nameof(amount));

        Amount = amount;
    }

    public static Money Zero => new Money(0);

    public Money Add(Money money)
    {
        return new Money(Amount + money.Amount);
    }

    public Money Subtract(Money money)
    {
        if (!CanSubtract(money))
            throw new InvalidOperationException("Insufficient funds.");

        return new Money(Amount - money.Amount);
    }

    public bool CanSubtract(Money money)
    {
        return Amount - money.Amount >= 0;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
    }
}
