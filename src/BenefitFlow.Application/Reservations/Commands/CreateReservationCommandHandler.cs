using BenefitFlow.Application.Abstractions.Configuration;
using BenefitFlow.Application.Abstractions.Caching;
using BenefitFlow.Application.Abstractions.Persistence;
using BenefitFlow.Application.Common;
using BenefitFlow.Application.DTOs;
using BenefitFlow.Domain.Common;
using BenefitFlow.Domain.Entities;
using BenefitFlow.Domain.Enums;
using BenefitFlow.Domain.ValueObjects;
using Mapster;
using MediatR;
using BenefitFlow.Application.Observability;

namespace BenefitFlow.Application.Reservations.Commands;

public sealed class CreateReservationCommandHandler(
    IBeneficiaryRepository beneficiaryRepository,
    IReservationRepository reservationRepository,
    IIdempotencyCache idempotencyCache,
    IReservationPolicy reservationPolicy,
    IUnitOfWork unitOfWork,
    TypeAdapterConfig mappingConfiguration,
    ReservationMetrics metrics,
    TimeProvider timeProvider) : IRequestHandler<CreateReservationCommand, Result<ReservationResponse>>
{
    public async Task<Result<ReservationResponse>> Handle(
        CreateReservationCommand request,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = request.IdempotencyKey.Trim();
        var cachedKey = await idempotencyCache.GetAsync(idempotencyKey, cancellationToken);
        if (cachedKey is not null)
        {
            if (!IsSameRequest(cachedKey, request))
            {
                return Result.Failure<ReservationResponse>(ApplicationErrors.IdempotencyKeyConflict);
            }

            var cachedReservation = await reservationRepository.GetByIdAsync(
                cachedKey.ReservationId,
                cancellationToken);
            if (cachedReservation is not null)
            {
                return Result.Success(cachedReservation.Adapt<ReservationResponse>(mappingConfiguration));
            }
        }

        var existing = await reservationRepository.GetByIdempotencyKeyAsync(
            idempotencyKey,
            cancellationToken);
        if (existing is not null)
        {
            if (IsSameRequest(existing, request))
            {
                await CacheIdempotencyKeyAsync(existing, cancellationToken);
                return Result.Success(existing.Adapt<ReservationResponse>(mappingConfiguration));
            }

            return Result.Failure<ReservationResponse>(ApplicationErrors.IdempotencyKeyConflict);
        }

        var beneficiary = await beneficiaryRepository.GetByIdAsync(
            request.BeneficiaryId,
            cancellationToken);
        if (beneficiary is null)
        {
            return Result.Failure<ReservationResponse>(ApplicationErrors.BeneficiaryNotFound);
        }

        if (beneficiary.Status != BeneficiaryStatus.Active)
        {
            return Result.Failure<ReservationResponse>(ApplicationErrors.BeneficiaryInactive);
        }

        var amount = Money.Create(request.Amount, request.Currency);
        if (amount.IsFailure)
        {
            return Result.Failure<ReservationResponse>(amount.Error!);
        }

        var createdAt = timeProvider.GetUtcNow();
        var reservation = BenefitReservation.Create(
            Guid.NewGuid(),
            request.BeneficiaryId,
            request.BenefitType,
            amount.Value,
            createdAt,
            createdAt.Add(reservationPolicy.PendingExpiration),
            idempotencyKey);
        if (reservation.IsFailure)
        {
            return Result.Failure<ReservationResponse>(reservation.Error!);
        }

        var createdReservation = reservation.Value!;
        reservationRepository.Add(createdReservation);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        metrics.ReservationCreated();
        await CacheIdempotencyKeyAsync(createdReservation, cancellationToken);

        return Result.Success(createdReservation.Adapt<ReservationResponse>(mappingConfiguration));
    }

    private Task CacheIdempotencyKeyAsync(
        BenefitReservation reservation,
        CancellationToken cancellationToken) =>
        idempotencyCache.SetAsync(
            reservation.IdempotencyKey,
            new IdempotencyCacheEntry(
                reservation.Id,
                reservation.BeneficiaryId,
                reservation.BenefitType,
                reservation.Amount.Amount,
                reservation.Amount.Currency),
            reservationPolicy.IdempotencyCacheTimeToLive,
            cancellationToken);

    private static bool IsSameRequest(
        IdempotencyCacheEntry existing,
        CreateReservationCommand request) =>
        existing.BeneficiaryId == request.BeneficiaryId &&
        existing.BenefitType == request.BenefitType &&
        existing.Amount == request.Amount &&
        string.Equals(
            existing.Currency,
            request.Currency.Trim(),
            StringComparison.OrdinalIgnoreCase);

    private static bool IsSameRequest(
        BenefitReservation existing,
        CreateReservationCommand request) =>
        existing.BeneficiaryId == request.BeneficiaryId &&
        existing.BenefitType == request.BenefitType &&
        existing.Amount.Amount == request.Amount &&
        string.Equals(
            existing.Amount.Currency,
            request.Currency.Trim(),
            StringComparison.OrdinalIgnoreCase);
}
