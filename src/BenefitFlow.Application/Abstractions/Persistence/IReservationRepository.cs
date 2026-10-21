using BenefitFlow.Domain.Entities;

namespace BenefitFlow.Application.Abstractions.Persistence;

public interface IReservationRepository
{
    Task<BenefitReservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<BenefitReservation?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    void Add(BenefitReservation reservation);
}
