using BenefitFlow.Application.DTOs;
using BenefitFlow.Domain.Entities;
using Mapster;

namespace BenefitFlow.Application.Mapping;

public static class ApplicationMappingConfiguration
{
    public static void Configure(TypeAdapterConfig configuration)
    {
        configuration.NewConfig<BenefitReservation, ReservationResponse>()
            .MapWith(reservation => new ReservationResponse(
                reservation.Id,
                reservation.BeneficiaryId,
                reservation.BenefitType,
                reservation.Amount.Amount,
                reservation.Amount.Currency,
                reservation.Status,
                reservation.CreatedAt,
                reservation.ExpiresAt));

        configuration.NewConfig<BenefitBalance, BenefitBalanceResponse>()
            .MapWith(balance => new BenefitBalanceResponse(
                balance.BeneficiaryId,
                balance.BenefitType,
                balance.AvailableAmount.Amount,
                balance.AvailableAmount.Currency));
    }
}
