using BenefitFlow.Domain.Common;

namespace BenefitFlow.Domain.ValueObjects;

public sealed record Money
{
    private Money()
    {
        Currency = string.Empty;
    }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; }

    public static Result<Money> Create(decimal amount, string? currency = "BRL")
    {
        var normalizedCurrency = currency?.Trim().ToUpperInvariant();
        if (amount < 0 ||
            normalizedCurrency is null ||
            normalizedCurrency.Length != 3 ||
            normalizedCurrency.Any(character => character is < 'A' or > 'Z'))
        {
            return Result.Failure<Money>(DomainErrors.InvalidMoney);
        }

        return Result.Success(new Money(amount, normalizedCurrency));
    }

    public Result<Money> Add(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (!HasSameCurrency(other))
        {
            return Result.Failure<Money>(DomainErrors.CurrencyMismatch);
        }

        if (other.Amount > decimal.MaxValue - Amount)
        {
            return Result.Failure<Money>(DomainErrors.BalanceOverflow);
        }

        return Result.Success(new Money(Amount + other.Amount, Currency));
    }

    public Result<Money> Subtract(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (!HasSameCurrency(other))
        {
            return Result.Failure<Money>(DomainErrors.CurrencyMismatch);
        }

        if (other.Amount > Amount)
        {
            return Result.Failure<Money>(DomainErrors.NegativeBalance);
        }

        return Result.Success(new Money(Amount - other.Amount, Currency));
    }

    private bool HasSameCurrency(Money other) =>
        string.Equals(Currency, other.Currency, StringComparison.Ordinal);
}
