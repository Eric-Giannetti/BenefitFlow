using BenefitFlow.Application.Reservations.Commands;
using BenefitFlow.Domain.Enums;
using BenefitFlow.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenefitFlow.Worker.Reservations;

public sealed partial class ReservationExpirationWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<ReservationExpirationWorker> logger) : BackgroundService
{
    private const int BatchSize = 100;
    private const int MaxConcurrencyAttempts = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var expiredReservations = await GetExpiredReservationIdsAsync(stoppingToken);
            foreach (var reservationId in expiredReservations)
            {
                await ExpireReservationAsync(reservationId, stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task<Guid[]> GetExpiredReservationIdsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BenefitFlowDbContext>();
        return await dbContext.Reservations
            .AsNoTracking()
            .Where(reservation =>
                reservation.Status == ReservationStatus.Pending &&
                reservation.ExpiresAt <= timeProvider.GetUtcNow())
            .OrderBy(reservation => reservation.ExpiresAt)
            .Select(reservation => reservation.Id)
            .Take(BatchSize)
            .ToArrayAsync(cancellationToken);
    }

    private async Task ExpireReservationAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxConcurrencyAttempts; attempt++)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            try
            {
                var result = await sender.Send(
                    new ProcessReservationCommand(reservationId),
                    cancellationToken);
                if (result.IsFailure)
                {
                    LogExpirationFailed(logger, reservationId, result.Error!.Code);
                }

                return;
            }
            catch (DbUpdateConcurrencyException exception) when (attempt < MaxConcurrencyAttempts)
            {
                LogExpirationConcurrencyRetry(logger, exception, reservationId, attempt);
                await Task.Delay(
                    TimeSpan.FromMilliseconds(100 * attempt),
                    cancellationToken);
            }
            catch (DbUpdateConcurrencyException exception)
            {
                LogExpirationConcurrencyExhausted(logger, exception, reservationId);
            }
        }
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Could not expire reservation {ReservationId}: {ErrorCode}")]
    private static partial void LogExpirationFailed(
        ILogger logger,
        Guid reservationId,
        string errorCode);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Retrying expiration after an optimistic concurrency conflict for {ReservationId}, attempt {Attempt}")]
    private static partial void LogExpirationConcurrencyRetry(
        ILogger logger,
        Exception exception,
        Guid reservationId,
        int attempt);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "Optimistic concurrency retries were exhausted while expiring {ReservationId}")]
    private static partial void LogExpirationConcurrencyExhausted(
        ILogger logger,
        Exception exception,
        Guid reservationId);
}
