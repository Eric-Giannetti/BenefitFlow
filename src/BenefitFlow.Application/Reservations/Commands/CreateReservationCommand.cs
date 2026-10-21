using BenefitFlow.Application.DTOs;
using BenefitFlow.Domain.Common;
using BenefitFlow.Domain.Enums;
using MediatR;

namespace BenefitFlow.Application.Reservations.Commands;

public sealed record CreateReservationCommand(
    Guid BeneficiaryId,
    BenefitType BenefitType,
    decimal Amount,
    string Currency,
    string IdempotencyKey) : IRequest<Result<ReservationResponse>>;
