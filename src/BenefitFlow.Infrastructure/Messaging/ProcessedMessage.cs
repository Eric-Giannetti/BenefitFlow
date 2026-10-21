namespace BenefitFlow.Infrastructure.Messaging;

public sealed class ProcessedMessage
{
    private ProcessedMessage()
    {
    }

    public ProcessedMessage(Guid messageId, DateTimeOffset processedAt)
    {
        MessageId = messageId;
        ProcessedAt = processedAt;
    }

    public Guid MessageId { get; private set; }

    public DateTimeOffset ProcessedAt { get; private set; }
}
