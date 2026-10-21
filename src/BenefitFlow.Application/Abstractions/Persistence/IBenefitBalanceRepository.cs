using BenefitFlow.Domain.Entities;
using BenefitFlow.Domain.Enums;

namespace BenefitFlow.Application.Abstractions.Persistence;

public interface IBenefitBalanceRepository
{
    Task<BenefitBalance?> GetByBeneficiaryAndTypeAsync(
        Guid beneficiaryId,
        BenefitType benefitType,
        CancellationToken cancellationToken = default);
}
