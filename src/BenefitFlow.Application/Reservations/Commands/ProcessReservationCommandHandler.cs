using BenefitFlow.Application.Abstractions.Caching;
using BenefitFlow.Application.Abstractions.External;
using BenefitFlow.Application.Abstractions.Persistence;
using BenefitFlow.Application.Common;
using BenefitFlow.Application.DTOs;
using BenefitFlow.Domain.Common;
using BenefitFlow.Domain.Entities;
using BenefitFlow.Domain.Enums;
using BenefitFlow.Application.Observability;
using MediatR;
using System.Diagnostics;

namespace BenefitFlow.Application.Reservations.Commands;

public sealed class ProcessReservationCommandHandler(
    IReservationRepository reservationRepository,
    IBeneficiaryRepository beneficiaryRepository,
    IBenefitBalanceRepository balanceRepository,
    IEligibilityService eligibilityService,
    IUnitOfWork unitOfWork,
    IReservationCache reservationCache,
    IBenefitBalanceCache balanceCache,
    ReservationMetrics metrics,
    TimeProvider timeProvider) : IRequestHandler<ProcessReservationCommand, Result<ReservationProcessingResult>>
{
    public async Task<Result<ReservationProcessingResult>> Handle(
        ProcessReservationCommand request,
        CancellationToken cancellationToken)
    {
        var reservation = await reservationRepository.GetByIdAsync(
            request.ReservationId,
            cancellationToken);
        if (reservation is null)
        {
            return Result.Failure<ReservationProcessingResult>(ApplicationErrors.ReservationNotFound);
        }

        if (reservation.Status != ReservationStatus.Pending)
        {
            return Result.Success(ToResult(reservation));
        }

        var processingStarted = Stopwatch.GetTimestamp();
        var now = timeProvider.GetUtcNow();
        if (now >= reservation.ExpiresAt)
        {
            var expiration = reservation.Expire(now);
            if (expiration.IsFailure)
            {
                return Result.Failure<ReservationProcessingResult>(expiration.Error!);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await reservationCache.RemoveAsync(reservation.Id, cancellationToken);
            metrics.RecordProcessingDuration(Stopwatch.GetElapsedTime(processingStarted).TotalSeconds);
            return Result.Success(ToResult(reservation));
        }

        var beneficiary = await beneficiaryRepository.GetByIdAsync(
            reservation.BeneficiaryId,
            cancellationToken);
        if (beneficiary is null || beneficiary.Status != BeneficiaryStatus.Active)
        {
            return await RejectAndSaveAsync(
                reservation,
                "Beneficiary is missing or inactive.",
                now,
                processingStarted,
                cancellationToken);
        }

        var eligibility = await eligibilityService.CheckAsync(
            reservation.BeneficiaryId,
            cancellationToken);
        if (!eligibility.IsEligible)
        {
            return await RejectAndSaveAsync(
                reservation,
                eligibility.Reason ?? "Beneficiary is not eligible.",
                now,
                processingStarted,
                cancellationToken);
        }

        var balance = await balanceRepository.GetByBeneficiaryAndTypeAsync(
            reservation.BeneficiaryId,
            reservation.BenefitType,
            cancellationToken);
        if (balance is null)
        {
            return await RejectAndSaveAsync(
                reservation,
                DomainErrors.InsufficientBalance.Message,
                now,
                processingStarted,
                cancellationToken);
        }

        var reserve = balance.Reserve(reservation.Amount);
        if (reserve.IsFailure)
        {
            return await RejectAndSaveAsync(
                reservation,
                reserve.Error!.Message,
                now,
                processingStarted,
                cancellationToken);
        }

        var confirmation = reservation.Confirm(now);
        if (confirmation.IsFailure)
        {
            return Result.Failure<ReservationProcessingResult>(confirmation.Error!);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await reservationCache.RemoveAsync(reservation.Id, cancellationToken);
        await balanceCache.RemoveAsync(
            reservation.BeneficiaryId,
            reservation.BenefitType,
            cancellationToken);
        metrics.ReservationConfirmed();
        metrics.RecordProcessingDuration(Stopwatch.GetElapsedTime(processingStarted).TotalSeconds);
        return Result.Success(ToResult(reservation));
    }

    private async Task<Result<ReservationProcessingResult>> RejectAndSaveAsync(
        BenefitReservation reservation,
        string reason,
        DateTimeOffset occurredAt,
        long processingStarted,
        CancellationToken cancellationToken)
    {
        var rejection = reservation.Reject(reason, occurredAt);
        if (rejection.IsFailure)
        {
            return Result.Failure<ReservationProcessingResult>(rejection.Error!);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await reservationCache.RemoveAsync(reservation.Id, cancellationToken);
        metrics.ReservationRejected();
        metrics.RecordProcessingDuration(Stopwatch.GetElapsedTime(processingStarted).TotalSeconds);
        return Result.Success(ToResult(reservation, reason));
    }

    private static ReservationProcessingResult ToResult(
        BenefitReservation reservation,
        string? rejectionReason = null) =>
        new(reservation.Id, reservation.Status, rejectionReason);
}
