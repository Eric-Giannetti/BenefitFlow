using BenefitFlow.Application.Abstractions.Caching;
using BenefitFlow.Application.Abstractions.Persistence;
using BenefitFlow.Application.Common;
using BenefitFlow.Domain.Common;
using BenefitFlow.Domain.Enums;
using MediatR;

namespace BenefitFlow.Application.Reservations.Commands;

public sealed class CancelReservationCommandHandler(
    IReservationRepository reservationRepository,
    IBenefitBalanceRepository balanceRepository,
    IUnitOfWork unitOfWork,
    IReservationCache reservationCache,
    IBenefitBalanceCache balanceCache,
    TimeProvider timeProvider) : IRequestHandler<CancelReservationCommand, Result>
{
    public async Task<Result> Handle(
        CancelReservationCommand request,
        CancellationToken cancellationToken)
    {
        var reservation = await reservationRepository.GetByIdAsync(
            request.ReservationId,
            cancellationToken);
        if (reservation is null)
        {
            return Result.Failure(ApplicationErrors.ReservationNotFound);
        }

        if (reservation.Status != ReservationStatus.Confirmed)
        {
            return Result.Failure(DomainErrors.InvalidReservationState);
        }

        var balance = await balanceRepository.GetByBeneficiaryAndTypeAsync(
            reservation.BeneficiaryId,
            reservation.BenefitType,
            cancellationToken);
        if (balance is null)
        {
            return Result.Failure(ApplicationErrors.BalanceNotFound);
        }

        var refund = balance.Refund(reservation.Amount);
        if (refund.IsFailure)
        {
            return refund;
        }

        var cancellation = reservation.Cancel(timeProvider.GetUtcNow());
        if (cancellation.IsFailure)
        {
            return cancellation;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await reservationCache.RemoveAsync(reservation.Id, cancellationToken);
        await balanceCache.RemoveAsync(
            reservation.BeneficiaryId,
            reservation.BenefitType,
            cancellationToken);
        return Result.Success();
    }
}
