using BenefitFlow.Domain.Common;
using BenefitFlow.Domain.Enums;
using BenefitFlow.Domain.Events;
using BenefitFlow.Domain.ValueObjects;

namespace BenefitFlow.Domain.Entities;

public sealed class BenefitReservation : AggregateRoot
{
    private BenefitReservation()
    {
        Amount = null!;
        IdempotencyKey = string.Empty;
    }

    private BenefitReservation(
        Guid id,
        Guid beneficiaryId,
        BenefitType benefitType,
        Money amount,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        string idempotencyKey)
    {
        Id = id;
        BeneficiaryId = beneficiaryId;
        BenefitType = benefitType;
        Amount = amount;
        Status = ReservationStatus.Pending;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        IdempotencyKey = idempotencyKey;
    }

    public Guid Id { get; private set; }

    public Guid BeneficiaryId { get; private set; }

    public BenefitType BenefitType { get; private set; }

    public Money Amount { get; private set; }

    public ReservationStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public string IdempotencyKey { get; private set; }

    public static Result<BenefitReservation> Create(
        Guid id,
        Guid beneficiaryId,
        BenefitType benefitType,
        Money? amount,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        string? idempotencyKey)
    {
        if (id == Guid.Empty ||
            beneficiaryId == Guid.Empty ||
            amount is null ||
            amount.Amount <= 0 ||
            createdAt == default ||
            expiresAt <= createdAt ||
            string.IsNullOrWhiteSpace(idempotencyKey) ||
            idempotencyKey.Trim().Length > 128)
        {
            return Result.Failure<BenefitReservation>(DomainErrors.InvalidReservation);
        }

        if (!Enum.IsDefined(benefitType))
        {
            return Result.Failure<BenefitReservation>(DomainErrors.InvalidBenefitType);
        }

        var reservation = new BenefitReservation(
            id,
            beneficiaryId,
            benefitType,
            amount,
            createdAt,
            expiresAt,
            idempotencyKey.Trim());
        reservation.AddDomainEvent(new ReservationRequested(
            id,
            beneficiaryId,
            benefitType,
            amount,
            reservation.IdempotencyKey,
            createdAt));

        return Result.Success(reservation);
    }

    public Result Confirm(DateTimeOffset occurredAt)
    {
        if (Status != ReservationStatus.Pending || occurredAt >= ExpiresAt)
        {
            return Result.Failure(DomainErrors.InvalidReservationState);
        }

        Status = ReservationStatus.Confirmed;
        AddDomainEvent(new ReservationConfirmed(Id, BeneficiaryId, occurredAt));
        return Result.Success();
    }

    public Result Reject(string? reason, DateTimeOffset occurredAt)
    {
        if (Status != ReservationStatus.Pending)
        {
            return Result.Failure(DomainErrors.InvalidReservationState);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure(DomainErrors.InvalidRejectionReason);
        }

        Status = ReservationStatus.Rejected;
        AddDomainEvent(new ReservationRejected(Id, BeneficiaryId, reason.Trim(), occurredAt));
        return Result.Success();
    }

    public Result Cancel(DateTimeOffset occurredAt)
    {
        if (Status != ReservationStatus.Confirmed)
        {
            return Result.Failure(DomainErrors.InvalidReservationState);
        }

        Status = ReservationStatus.Cancelled;
        AddDomainEvent(new ReservationCancelled(Id, BeneficiaryId, occurredAt));
        return Result.Success();
    }

    public Result Expire(DateTimeOffset occurredAt)
    {
        if (Status != ReservationStatus.Pending)
        {
            return Result.Failure(DomainErrors.InvalidReservationState);
        }

        if (occurredAt < ExpiresAt)
        {
            return Result.Failure(DomainErrors.ReservationNotExpired);
        }

        Status = ReservationStatus.Expired;
        AddDomainEvent(new ReservationExpired(Id, BeneficiaryId, occurredAt));
        return Result.Success();
    }
}
