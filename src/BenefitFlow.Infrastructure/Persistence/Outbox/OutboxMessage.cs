using System.Diagnostics;
using System.Text.Json;
using BenefitFlow.Domain.Common;

namespace BenefitFlow.Infrastructure.Persistence.Outbox;

public sealed class OutboxMessage
{
    private OutboxMessage()
    {
        EventType = string.Empty;
        Payload = string.Empty;
    }

    private OutboxMessage(
        Guid id,
        string eventType,
        string payload,
        DateTimeOffset occurredAt,
        string? traceParent,
        string? traceState)
    {
        Id = id;
        EventType = eventType;
        Payload = payload;
        OccurredAt = occurredAt;
        TraceParent = traceParent;
        TraceState = traceState;
    }

    public Guid Id { get; private set; }

    public string EventType { get; private set; }

    public string Payload { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public string? TraceParent { get; private set; }

    public string? TraceState { get; private set; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    public int Attempts { get; private set; }

    public string? LastError { get; private set; }

    public static OutboxMessage From(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        var eventType = domainEvent.GetType();
        return new OutboxMessage(
            Guid.NewGuid(),
            eventType.AssemblyQualifiedName ?? eventType.FullName ?? eventType.Name,
            JsonSerializer.Serialize(domainEvent, eventType),
            domainEvent.OccurredAt,
            Activity.Current?.Id,
            Activity.Current?.TraceStateString);
    }

    public void MarkPublished(DateTimeOffset processedAt)
    {
        if (ProcessedAt is not null)
        {
            return;
        }

        Attempts++;
        ProcessedAt = processedAt;
        LastError = null;
    }
}
