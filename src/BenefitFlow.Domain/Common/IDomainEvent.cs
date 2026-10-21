namespace BenefitFlow.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
