namespace BenefitFlow.Domain.Common;

public abstract record DomainEvent(DateTimeOffset OccurredAt) : IDomainEvent;
