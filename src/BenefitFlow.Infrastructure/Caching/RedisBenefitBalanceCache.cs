using BenefitFlow.Application.Abstractions.Caching;
using BenefitFlow.Application.DTOs;
using BenefitFlow.Domain.Enums;
using StackExchange.Redis;

namespace BenefitFlow.Infrastructure.Caching;

public sealed class RedisBenefitBalanceCache(RedisCacheOperations cacheOperations) : IBenefitBalanceCache
{
    public async Task<BenefitBalanceResponse?> GetAsync(
        Guid beneficiaryId,
        BenefitType benefitType,
        CancellationToken cancellationToken = default) =>
        await cacheOperations.GetAsync<BenefitBalanceResponse>(
            Key(beneficiaryId, benefitType),
            cancellationToken);

    public Task SetAsync(
        BenefitBalanceResponse balance,
        TimeSpan timeToLive,
        CancellationToken cancellationToken = default) =>
        cacheOperations.SetAsync(
            Key(balance.BeneficiaryId, balance.BenefitType),
            balance,
            timeToLive,
            cancellationToken);

    public Task RemoveAsync(
        Guid beneficiaryId,
        BenefitType benefitType,
        CancellationToken cancellationToken = default) =>
        cacheOperations.RemoveAsync(Key(beneficiaryId, benefitType), cancellationToken);

    private static RedisKey Key(Guid beneficiaryId, BenefitType benefitType) =>
        $"benefitflow:balance:{beneficiaryId:N}:{benefitType}";
}
