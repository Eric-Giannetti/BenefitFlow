using BenefitFlow.Application.Abstractions.Persistence;
using BenefitFlow.Domain.Entities;
using BenefitFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BenefitFlow.Infrastructure.Persistence.Repositories;

public sealed class BenefitBalanceRepository(BenefitFlowDbContext dbContext) : IBenefitBalanceRepository
{
    public Task<BenefitBalance?> GetByBeneficiaryAndTypeAsync(
        Guid beneficiaryId,
        BenefitType benefitType,
        CancellationToken cancellationToken = default) =>
        dbContext.BenefitBalances.SingleOrDefaultAsync(
            balance => balance.BeneficiaryId == beneficiaryId && balance.BenefitType == benefitType,
            cancellationToken);
}
