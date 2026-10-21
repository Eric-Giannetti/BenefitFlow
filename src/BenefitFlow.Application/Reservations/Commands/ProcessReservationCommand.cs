using BenefitFlow.Application.DTOs;
using BenefitFlow.Domain.Common;
using MediatR;

namespace BenefitFlow.Application.Reservations.Commands;

public sealed record ProcessReservationCommand(Guid ReservationId)
    : IRequest<Result<ReservationProcessingResult>>;
