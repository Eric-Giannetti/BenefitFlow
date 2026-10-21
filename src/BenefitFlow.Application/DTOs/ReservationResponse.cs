using BenefitFlow.Domain.Enums;

namespace BenefitFlow.Application.DTOs;

public sealed record ReservationResponse(
    Guid Id,
    Guid BeneficiaryId,
    BenefitType BenefitType,
    decimal Amount,
    string Currency,
    ReservationStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt);
