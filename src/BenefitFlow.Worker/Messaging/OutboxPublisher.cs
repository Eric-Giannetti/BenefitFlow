using BenefitFlow.Infrastructure.Messaging;
using BenefitFlow.Application.Observability;
using BenefitFlow.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using RabbitMQ.Client.Exceptions;
using Polly.Registry;

namespace BenefitFlow.Worker.Messaging;

public sealed partial class OutboxPublisher(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ResiliencePipelineProvider<string> pipelineProvider,
    ReservationMetrics metrics,
    ILogger<OutboxPublisher> logger) : BackgroundService
{
    private const int BatchSize = 25;
    private static readonly ActivitySource OutboxActivitySource = new("BenefitFlow.Outbox");
    private readonly ResiliencePipeline _rabbitPipeline = pipelineProvider.GetPipeline("rabbitmq");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var publishedCount = await PublishBatchAsync(stoppingToken);
                if (publishedCount == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
                else
                {
                    LogPublishedBatch(logger, publishedCount);
                }
            }
            catch (MassTransitException exception)
            {
                await WaitBeforeRetryAsync(exception, stoppingToken);
            }
            catch (BrokerUnreachableException exception)
            {
                await WaitBeforeRetryAsync(exception, stoppingToken);
            }
            catch (TimeoutRejectedException exception)
            {
                await WaitBeforeRetryAsync(exception, stoppingToken);
            }
            catch (BrokenCircuitException exception)
            {
                await WaitBeforeRetryAsync(exception, stoppingToken);
            }
        }
    }

    private async Task WaitBeforeRetryAsync(Exception exception, CancellationToken cancellationToken)
    {
        LogPublishFailure(logger, exception);
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }

    private async Task<int> PublishBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BenefitFlowDbContext>();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var messages = await dbContext.OutboxMessages
            .FromSqlInterpolated($"""
                SELECT *
                FROM outbox_messages
                WHERE "ProcessedAt" IS NULL
                ORDER BY "OccurredAt"
                LIMIT {BatchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
        {
            metrics.SetPendingOutboxCount(
                await dbContext.OutboxMessages.LongCountAsync(
                    message => message.ProcessedAt == null,
                    cancellationToken));
            await transaction.CommitAsync(cancellationToken);
            return 0;
        }

        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        foreach (var message in messages)
        {
            var parentContext = ActivityContext.TryParse(
                message.TraceParent,
                message.TraceState,
                out var parsedContext)
                ? parsedContext
                : default;
            using var activity = OutboxActivitySource.StartActivity(
                "outbox.publish",
                ActivityKind.Producer,
                parentContext);
            var envelope = new OutboxEvent(
                message.Id,
                message.EventType,
                message.Payload,
                message.OccurredAt);
            await _rabbitPipeline.ExecuteAsync(
                token => new ValueTask(publishEndpoint.Publish(envelope, token)),
                cancellationToken);
            message.MarkPublished(timeProvider.GetUtcNow());
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        metrics.SetPendingOutboxCount(
            await dbContext.OutboxMessages.LongCountAsync(
                message => message.ProcessedAt == null,
                cancellationToken));
        await transaction.CommitAsync(cancellationToken);
        return messages.Count;
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Published {MessageCount} outbox messages")]
    private static partial void LogPublishedBatch(ILogger logger, int messageCount);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Outbox publish failed; the message remains pending for retry")]
    private static partial void LogPublishFailure(ILogger logger, Exception exception);
}
