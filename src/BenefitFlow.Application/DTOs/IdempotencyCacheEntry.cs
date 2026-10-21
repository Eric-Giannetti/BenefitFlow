using BenefitFlow.Domain.Enums;

namespace BenefitFlow.Application.DTOs;

public sealed record IdempotencyCacheEntry(
    Guid ReservationId,
    Guid BeneficiaryId,
    BenefitType BenefitType,
    decimal Amount,
    string Currency);
