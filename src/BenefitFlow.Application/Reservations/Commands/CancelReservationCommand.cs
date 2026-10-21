using BenefitFlow.Domain.Common;
using MediatR;

namespace BenefitFlow.Application.Reservations.Commands;

public sealed record CancelReservationCommand(Guid ReservationId) : IRequest<Result>;
