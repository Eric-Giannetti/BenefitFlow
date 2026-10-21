using BenefitFlow.Domain.Common;

namespace BenefitFlow.Domain.Events;

public sealed record ReservationConfirmed(
    Guid ReservationId,
    Guid BeneficiaryId,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);
