using BenefitFlow.Application.DTOs;
using BenefitFlow.Domain.Common;
using MediatR;

namespace BenefitFlow.Application.Reservations.Queries;

public sealed record GetReservationQuery(Guid ReservationId)
    : IRequest<Result<ReservationResponse>>;
