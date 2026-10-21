using BenefitFlow.Application.Abstractions.Persistence;
using BenefitFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BenefitFlow.Infrastructure.Persistence.Repositories;

public sealed class BeneficiaryRepository(BenefitFlowDbContext dbContext) : IBeneficiaryRepository
{
    public Task<Beneficiary?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Beneficiaries.SingleOrDefaultAsync(
            beneficiary => beneficiary.Id == id,
            cancellationToken);
}
