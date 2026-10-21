using BenefitFlow.Application.Abstractions.Caching;
using BenefitFlow.Application.Abstractions.Configuration;
using BenefitFlow.Application.Abstractions.Persistence;
using BenefitFlow.Application.Common;
using BenefitFlow.Application.DTOs;
using BenefitFlow.Domain.Common;
using Mapster;
using MediatR;
using BenefitFlow.Application.Observability;

namespace BenefitFlow.Application.Balances.Queries;

public sealed class GetBenefitBalanceQueryHandler(
    IBenefitBalanceRepository balanceRepository,
    IBenefitBalanceCache balanceCache,
    IReservationPolicy reservationPolicy,
    TypeAdapterConfig mappingConfiguration,
    ReservationMetrics metrics) : IRequestHandler<GetBenefitBalanceQuery, Result<BenefitBalanceResponse>>
{
    public async Task<Result<BenefitBalanceResponse>> Handle(
        GetBenefitBalanceQuery request,
        CancellationToken cancellationToken)
    {
        var cached = await balanceCache.GetAsync(
            request.BeneficiaryId,
            request.BenefitType,
            cancellationToken);
        if (cached is not null)
        {
            metrics.CacheHit("balance");
            return Result.Success(cached);
        }

        metrics.CacheMiss("balance");
        var balance = await balanceRepository.GetByBeneficiaryAndTypeAsync(
            request.BeneficiaryId,
            request.BenefitType,
            cancellationToken);
        if (balance is null)
        {
            return Result.Failure<BenefitBalanceResponse>(ApplicationErrors.BalanceNotFound);
        }

        var response = balance.Adapt<BenefitBalanceResponse>(mappingConfiguration);
        await balanceCache.SetAsync(response, reservationPolicy.ReadCacheTimeToLive, cancellationToken);
        return Result.Success(response);
    }
}
