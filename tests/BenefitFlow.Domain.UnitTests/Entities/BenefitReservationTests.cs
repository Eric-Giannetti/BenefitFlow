using BenefitFlow.Domain.Entities;
using BenefitFlow.Domain.Enums;
using BenefitFlow.Domain.Events;
using BenefitFlow.Domain.ValueObjects;
using BenefitFlow.Domain.Common;
using FluentAssertions;

namespace BenefitFlow.Domain.UnitTests.Entities;

public sealed class BenefitReservationTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_adds_requested_event_and_starts_pending()
    {
        var reservation = CreateReservation();

        reservation.IsSuccess.Should().BeTrue();
        reservation.Value!.Status.Should().Be(ReservationStatus.Pending);
        reservation.Value.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<ReservationRequested>();
    }

    [Fact]
    public void Confirm_can_only_happen_once_while_pending_and_before_expiration()
    {
        var reservation = CreateReservation().Value!;

        reservation.Confirm(CreatedAt.AddMinutes(2)).IsSuccess.Should().BeTrue();
        reservation.Confirm(CreatedAt.AddMinutes(3)).IsFailure.Should().BeTrue();
        reservation.Status.Should().Be(ReservationStatus.Confirmed);
        reservation.DomainEvents.Should().ContainSingle(domainEvent => domainEvent is ReservationConfirmed);
    }

    [Fact]
    public void Reject_and_cancel_raise_the_matching_events()
    {
        var rejected = CreateReservation().Value!;
        rejected.Reject(" Insufficient balance ", CreatedAt.AddMinutes(1)).IsSuccess.Should().BeTrue();

        var cancelled = CreateReservation().Value!;
        cancelled.Confirm(CreatedAt.AddMinutes(1)).IsSuccess.Should().BeTrue();
        cancelled.Cancel(CreatedAt.AddMinutes(2)).IsSuccess.Should().BeTrue();

        rejected.DomainEvents.Should().ContainSingle(domainEvent => domainEvent is ReservationRejected);
        cancelled.DomainEvents.Should().ContainSingle(domainEvent => domainEvent is ReservationCancelled);
        cancelled.Cancel(CreatedAt.AddMinutes(3)).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Expire_requires_pending_status_and_expiration_time()
    {
        var reservation = CreateReservation().Value!;

        reservation.Expire(CreatedAt.AddMinutes(4)).Error!.Code.Should().Be("reservation.not_expired");
        reservation.Expire(CreatedAt.AddMinutes(5)).IsSuccess.Should().BeTrue();
        reservation.Status.Should().Be(ReservationStatus.Expired);
        reservation.DomainEvents.Should().ContainSingle(domainEvent => domainEvent is ReservationExpired);
    }

    [Fact]
    public void Create_rejects_non_positive_amount_and_expiration_before_creation()
    {
        var zeroAmount = Money.Create(0m).Value!;
        var zeroResult = BenefitReservation.Create(
            Guid.NewGuid(), Guid.NewGuid(), BenefitType.Exam, zeroAmount,
            CreatedAt, CreatedAt.AddMinutes(5), "idempotency-key");
        var invalidTime = BenefitReservation.Create(
            Guid.NewGuid(), Guid.NewGuid(), BenefitType.Exam, Money.Create(10m).Value!,
            CreatedAt, CreatedAt, "idempotency-key");

        zeroResult.IsFailure.Should().BeTrue();
        invalidTime.IsFailure.Should().BeTrue();
    }

    private static Result<BenefitReservation> CreateReservation() =>
        BenefitReservation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            BenefitType.Consultation,
            Money.Create(125.50m).Value!,
            CreatedAt,
            CreatedAt.AddMinutes(5),
            Guid.NewGuid().ToString("N"));
}
