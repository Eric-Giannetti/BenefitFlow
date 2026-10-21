using BenefitFlow.Application.Abstractions.Caching;
using BenefitFlow.Application.Abstractions.Configuration;
using BenefitFlow.Application.Abstractions.Persistence;
using BenefitFlow.Application.Common;
using BenefitFlow.Application.DTOs;
using BenefitFlow.Domain.Common;
using Mapster;
using MediatR;
using BenefitFlow.Application.Observability;

namespace BenefitFlow.Application.Reservations.Queries;

public sealed class GetReservationQueryHandler(
    IReservationRepository reservationRepository,
    IReservationCache reservationCache,
    IReservationPolicy reservationPolicy,
    TypeAdapterConfig mappingConfiguration,
    ReservationMetrics metrics) : IRequestHandler<GetReservationQuery, Result<ReservationResponse>>
{
    public async Task<Result<ReservationResponse>> Handle(
        GetReservationQuery request,
        CancellationToken cancellationToken)
    {
        var cached = await reservationCache.GetAsync(request.ReservationId, cancellationToken);
        if (cached is not null)
        {
            metrics.CacheHit("reservation");
            return Result.Success(cached);
        }

        metrics.CacheMiss("reservation");
        var reservation = await reservationRepository.GetByIdAsync(
            request.ReservationId,
            cancellationToken);
        if (reservation is null)
        {
            return Result.Failure<ReservationResponse>(ApplicationErrors.ReservationNotFound);
        }

        var response = reservation.Adapt<ReservationResponse>(mappingConfiguration);
        await reservationCache.SetAsync(response, reservationPolicy.ReadCacheTimeToLive, cancellationToken);
        return Result.Success(response);
    }
}
