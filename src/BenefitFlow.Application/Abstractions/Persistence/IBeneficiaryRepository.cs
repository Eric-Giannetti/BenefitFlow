using BenefitFlow.Domain.Entities;

namespace BenefitFlow.Application.Abstractions.Persistence;

public interface IBeneficiaryRepository
{
    Task<Beneficiary?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
