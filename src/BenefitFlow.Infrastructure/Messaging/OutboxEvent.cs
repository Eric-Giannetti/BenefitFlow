namespace BenefitFlow.Infrastructure.Messaging;

public sealed record OutboxEvent(
    Guid MessageId,
    string EventType,
    string Payload,
    DateTimeOffset OccurredAt);
