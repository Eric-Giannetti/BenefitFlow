using BenefitFlow.Domain.Common;

namespace BenefitFlow.Domain.Events;

public sealed record ReservationRejected(
    Guid ReservationId,
    Guid BeneficiaryId,
    string Reason,
    DateTimeOffset OccurredAt) : DomainEvent(OccurredAt);
