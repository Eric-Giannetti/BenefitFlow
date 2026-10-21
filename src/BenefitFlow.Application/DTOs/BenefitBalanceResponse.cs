using BenefitFlow.Domain.Enums;

namespace BenefitFlow.Application.DTOs;

public sealed record BenefitBalanceResponse(
    Guid BeneficiaryId,
    BenefitType BenefitType,
    decimal Amount,
    string Currency);
