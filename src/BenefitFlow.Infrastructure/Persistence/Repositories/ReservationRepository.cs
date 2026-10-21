using BenefitFlow.Application.Abstractions.Persistence;
using BenefitFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BenefitFlow.Infrastructure.Persistence.Repositories;

public sealed class ReservationRepository(BenefitFlowDbContext dbContext) : IReservationRepository
{
    public Task<BenefitReservation?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        dbContext.Reservations.SingleOrDefaultAsync(
            reservation => reservation.Id == id,
            cancellationToken);

    public Task<BenefitReservation?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        dbContext.Reservations.SingleOrDefaultAsync(
            reservation => reservation.IdempotencyKey == idempotencyKey,
            cancellationToken);

    public void Add(BenefitReservation reservation)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        dbContext.Reservations.Add(reservation);
    }
}
