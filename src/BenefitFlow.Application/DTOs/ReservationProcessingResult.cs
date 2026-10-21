using BenefitFlow.Domain.Enums;

namespace BenefitFlow.Application.DTOs;

public sealed record ReservationProcessingResult(
    Guid ReservationId,
    ReservationStatus Status,
    string? RejectionReason);
