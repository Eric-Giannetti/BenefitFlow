using BenefitFlow.Domain.Common;
using BenefitFlow.Domain.Enums;
using BenefitFlow.Domain.ValueObjects;

namespace BenefitFlow.Domain.Entities;

public sealed class BenefitBalance : AggregateRoot
{
    private BenefitBalance()
    {
        AvailableAmount = null!;
    }

    private BenefitBalance(Guid id, Guid beneficiaryId, BenefitType benefitType, Money availableAmount)
    {
        Id = id;
        BeneficiaryId = beneficiaryId;
        BenefitType = benefitType;
        AvailableAmount = availableAmount;
    }

    public Guid Id { get; private set; }

    public Guid BeneficiaryId { get; private set; }

    public BenefitType BenefitType { get; private set; }

    public Money AvailableAmount { get; private set; }

    public static Result<BenefitBalance> Open(
        Guid id,
        Guid beneficiaryId,
        BenefitType benefitType,
        Money? initialAmount)
    {
        if (id == Guid.Empty ||
            beneficiaryId == Guid.Empty ||
            initialAmount is null)
        {
            return Result.Failure<BenefitBalance>(DomainErrors.InvalidBalance);
        }

        if (!Enum.IsDefined(benefitType))
        {
            return Result.Failure<BenefitBalance>(DomainErrors.InvalidBenefitType);
        }

        return Result.Success(
            new BenefitBalance(id, beneficiaryId, benefitType, initialAmount));
    }

    public Result Reserve(Money amount)
    {
        ArgumentNullException.ThrowIfNull(amount);
        if (!string.Equals(AvailableAmount.Currency, amount.Currency, StringComparison.Ordinal))
        {
            return Result.Failure(DomainErrors.CurrencyMismatch);
        }

        if (amount.Amount <= 0)
        {
            return Result.Failure(DomainErrors.InvalidMoney);
        }

        var remaining = AvailableAmount.Subtract(amount);
        if (remaining.IsFailure)
        {
            return Result.Failure(DomainErrors.InsufficientBalance);
        }

        AvailableAmount = remaining.Value!;
        return Result.Success();
    }

    public Result Refund(Money amount)
    {
        ArgumentNullException.ThrowIfNull(amount);
        if (amount.Amount <= 0)
        {
            return Result.Failure(DomainErrors.InvalidMoney);
        }

        var updated = AvailableAmount.Add(amount);
        if (updated.IsFailure)
        {
            return Result.Failure(updated.Error!);
        }

        AvailableAmount = updated.Value!;
        return Result.Success();
    }
}
