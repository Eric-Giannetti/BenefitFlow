using BenefitFlow.Application.Abstractions.Configuration;
using Microsoft.Extensions.Options;

namespace BenefitFlow.Infrastructure.Configuration;

public sealed class ReservationPolicy(IOptions<ReservationOptions> options) : IReservationPolicy
{
    public TimeSpan PendingExpiration =>
        TimeSpan.FromMinutes(options.Value.PendingExpirationMinutes);

    public TimeSpan ReadCacheTimeToLive =>
        TimeSpan.FromSeconds(options.Value.ReadCacheTimeToLiveSeconds);

    public TimeSpan IdempotencyCacheTimeToLive =>
        TimeSpan.FromHours(options.Value.IdempotencyCacheTimeToLiveHours);
}
