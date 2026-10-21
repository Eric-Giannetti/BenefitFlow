using BenefitFlow.Domain.Enums;

namespace BenefitFlow.Api.Models;

public sealed record CreateReservationRequest(
    Guid BeneficiaryId,
    BenefitType BenefitType,
    decimal Amount,
    string Currency = "BRL");
