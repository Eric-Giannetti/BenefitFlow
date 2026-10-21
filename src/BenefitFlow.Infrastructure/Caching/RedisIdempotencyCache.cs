using System.Security.Cryptography;
using System.Text;
using BenefitFlow.Application.Abstractions.Caching;
using BenefitFlow.Application.DTOs;
using StackExchange.Redis;

namespace BenefitFlow.Infrastructure.Caching;

public sealed class RedisIdempotencyCache(RedisCacheOperations cacheOperations) : IIdempotencyCache
{
    public Task<IdempotencyCacheEntry?> GetAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        cacheOperations.GetAsync<IdempotencyCacheEntry>(Key(idempotencyKey), cancellationToken);

    public Task SetAsync(
        string idempotencyKey,
        IdempotencyCacheEntry entry,
        TimeSpan timeToLive,
        CancellationToken cancellationToken = default) =>
        cacheOperations.SetAsync(Key(idempotencyKey), entry, timeToLive, cancellationToken);

    private static RedisKey Key(string idempotencyKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(idempotencyKey)));
        return $"benefitflow:idempotency:{hash}";
    }
}
