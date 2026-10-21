using BenefitFlow.Application.DTOs;
using BenefitFlow.Domain.Enums;

namespace BenefitFlow.Application.Abstractions.Caching;

public interface IBenefitBalanceCache
{
    Task<BenefitBalanceResponse?> GetAsync(
        Guid beneficiaryId,
        BenefitType benefitType,
        CancellationToken cancellationToken = default);

    Task SetAsync(
        BenefitBalanceResponse balance,
        TimeSpan timeToLive,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        Guid beneficiaryId,
        BenefitType benefitType,
        CancellationToken cancellationToken = default);
}
