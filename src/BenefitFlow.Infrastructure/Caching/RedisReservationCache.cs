using BenefitFlow.Application.Abstractions.Caching;
using BenefitFlow.Application.DTOs;
using StackExchange.Redis;

namespace BenefitFlow.Infrastructure.Caching;

public sealed class RedisReservationCache(RedisCacheOperations cacheOperations) : IReservationCache
{
    public async Task<ReservationResponse?> GetAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default) =>
        await cacheOperations.GetAsync<ReservationResponse>(Key(reservationId), cancellationToken);

    public Task SetAsync(
        ReservationResponse reservation,
        TimeSpan timeToLive,
        CancellationToken cancellationToken = default) =>
        cacheOperations.SetAsync(Key(reservation.Id), reservation, timeToLive, cancellationToken);

    public Task RemoveAsync(Guid reservationId, CancellationToken cancellationToken = default) =>
        cacheOperations.RemoveAsync(Key(reservationId), cancellationToken);

    private static RedisKey Key(Guid reservationId) => $"benefitflow:reservation:{reservationId:N}";
}
