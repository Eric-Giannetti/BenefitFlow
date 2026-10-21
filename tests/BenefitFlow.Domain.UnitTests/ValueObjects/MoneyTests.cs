using BenefitFlow.Domain.ValueObjects;
using FluentAssertions;

namespace BenefitFlow.Domain.UnitTests.ValueObjects;

public sealed class MoneyTests
{
    [Fact]
    public void Create_normalizes_currency()
    {
        var result = Money.Create(12.50m, "brl");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(12.50m);
        result.Value.Currency.Should().Be("BRL");
    }

    [Fact]
    public void Create_rejects_negative_amount()
    {
        var result = Money.Create(-1m);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_rejects_invalid_currency()
    {
        var result = Money.Create(12.50m, "B1L");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Subtract_rejects_negative_result_and_currency_mismatch()
    {
        var amount = Money.Create(10m).Value!;

        amount.Subtract(Money.Create(11m).Value!).Error!.Code.Should().Be("balance.negative");
        amount.Subtract(Money.Create(1m, "USD").Value!).Error!.Code.Should().Be("money.currency_mismatch");
    }
}
