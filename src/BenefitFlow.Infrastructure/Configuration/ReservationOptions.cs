namespace BenefitFlow.Infrastructure.Configuration;

public sealed class ReservationOptions
{
    public int PendingExpirationMinutes { get; set; } = 5;

    public int ReadCacheTimeToLiveSeconds { get; set; } = 30;

    public int IdempotencyCacheTimeToLiveHours { get; set; } = 24;
}
