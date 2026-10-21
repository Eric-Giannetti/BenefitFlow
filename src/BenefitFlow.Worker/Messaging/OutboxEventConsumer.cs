using System.Text.Json;
using BenefitFlow.Application.Abstractions.Persistence;
using BenefitFlow.Application.Reservations.Commands;
using BenefitFlow.Domain.Events;
using BenefitFlow.Domain.Enums;
using BenefitFlow.Infrastructure.Messaging;
using BenefitFlow.Infrastructure.Persistence;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BenefitFlow.Worker.Messaging;

public sealed partial class OutboxEventConsumer(
    BenefitFlowDbContext dbContext,
    ISender sender,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<OutboxEventConsumer> logger) : IConsumer<OutboxEvent>
{
    public async Task Consume(ConsumeContext<OutboxEvent> context)
    {
        var message = context.Message;
        var alreadyProcessed = await dbContext.ProcessedMessages
            .AnyAsync(processed => processed.MessageId == message.MessageId, context.CancellationToken);
        if (alreadyProcessed)
        {
            return;
        }

        if (IsReservationRequested(message.EventType))
        {
            using var document = JsonDocument.Parse(message.Payload);
            if (!document.RootElement.TryGetProperty(nameof(ReservationRequested.ReservationId), out var idValue) ||
                !idValue.TryGetGuid(out var reservationId))
            {
                throw new InvalidDataException("ReservationRequested payload has no valid ReservationId.");
            }

            if (!document.RootElement.TryGetProperty(nameof(ReservationRequested.BeneficiaryId), out var beneficiaryValue) ||
                !beneficiaryValue.TryGetGuid(out var beneficiaryId))
            {
                throw new InvalidDataException("ReservationRequested payload has no valid BeneficiaryId.");
            }

            var result = await sender.Send(
                new ProcessReservationCommand(reservationId),
                context.CancellationToken);
            if (result.IsFailure)
            {
                throw new InvalidOperationException(
                    $"Reservation {reservationId} could not be processed: {result.Error!.Code}");
            }

            LogReservationProcessed(logger, reservationId, beneficiaryId, result.Value!.Status);
        }

        dbContext.ProcessedMessages.Add(new ProcessedMessage(message.MessageId, timeProvider.GetUtcNow()));
        await unitOfWork.SaveChangesAsync(context.CancellationToken);
    }

    private static bool IsReservationRequested(string eventType) =>
        string.Equals(
            eventType,
            typeof(ReservationRequested).AssemblyQualifiedName,
            StringComparison.Ordinal);

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Processed reservation {ReservationId} for beneficiary {BeneficiaryId} with status {Status}")]
    private static partial void LogReservationProcessed(
        ILogger logger,
        Guid reservationId,
        Guid beneficiaryId,
        ReservationStatus status);
}
