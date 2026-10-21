using BenefitFlow.Domain.Common;
using BenefitFlow.Domain.Enums;
using BenefitFlow.Domain.ValueObjects;

namespace BenefitFlow.Domain.Events;

public sealed record ReservationRequested(
    Guid ReservationId,
    Guid BeneficiaryId,
    BenefitType BenefitType,
    Money Amount,
    string IdempotencyKey,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);
