using BenefitFlow.Domain.Entities;
using BenefitFlow.Domain.Enums;
using BenefitFlow.Domain.ValueObjects;
using FluentAssertions;

namespace BenefitFlow.Domain.UnitTests.Entities;

public sealed class BenefitBalanceTests
{
    [Fact]
    public void Reserve_decreases_balance_and_refund_restores_it()
    {
        var balance = OpenBalance(100m);

        balance.Reserve(Money.Create(30m).Value!).IsSuccess.Should().BeTrue();
        balance.AvailableAmount.Amount.Should().Be(70m);
        balance.Refund(Money.Create(30m).Value!).IsSuccess.Should().BeTrue();
        balance.AvailableAmount.Amount.Should().Be(100m);
    }

    [Fact]
    public void Reserve_rejects_insufficient_amount_currency_mismatch_and_zero()
    {
        var balance = OpenBalance(100m);

        balance.Reserve(Money.Create(101m).Value!).Error!.Code.Should().Be("balance.insufficient");
        balance.Reserve(Money.Create(10m, "USD").Value!).Error!.Code.Should().Be("money.currency_mismatch");
        balance.Reserve(Money.Create(0m).Value!).Error!.Code.Should().Be("money.invalid_amount");
        balance.AvailableAmount.Amount.Should().Be(100m);
    }

    [Fact]
    public void Refund_rejects_amounts_that_overflow_supported_balance()
    {
        var balance = OpenBalance(decimal.MaxValue);

        balance.Refund(Money.Create(1m).Value!).Error!.Code.Should().Be("balance.overflow");
        balance.AvailableAmount.Amount.Should().Be(decimal.MaxValue);
    }

    private static BenefitBalance OpenBalance(decimal amount) =>
        BenefitBalance.Open(
            Guid.NewGuid(),
            Guid.NewGuid(),
            BenefitType.Medication,
            Money.Create(amount).Value!).Value!;
}
