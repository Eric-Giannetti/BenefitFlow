using BenefitFlow.Application.DTOs;

namespace BenefitFlow.Application.Abstractions.Caching;

public interface IIdempotencyCache
{
    Task<IdempotencyCacheEntry?> GetAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task SetAsync(
        string idempotencyKey,
        IdempotencyCacheEntry entry,
        TimeSpan timeToLive,
        CancellationToken cancellationToken = default);
}
