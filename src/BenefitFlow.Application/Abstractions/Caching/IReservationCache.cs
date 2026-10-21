using BenefitFlow.Application.DTOs;

namespace BenefitFlow.Application.Abstractions.Caching;

public interface IReservationCache
{
    Task<ReservationResponse?> GetAsync(Guid reservationId, CancellationToken cancellationToken = default);

    Task SetAsync(
        ReservationResponse reservation,
        TimeSpan timeToLive,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(Guid reservationId, CancellationToken cancellationToken = default);
}
