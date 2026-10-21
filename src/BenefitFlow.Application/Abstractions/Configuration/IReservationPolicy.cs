namespace BenefitFlow.Application.Abstractions.Configuration;

public interface IReservationPolicy
{
    TimeSpan PendingExpiration { get; }

    TimeSpan ReadCacheTimeToLive { get; }

    TimeSpan IdempotencyCacheTimeToLive { get; }
}
