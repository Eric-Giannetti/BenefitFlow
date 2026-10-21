using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace BenefitFlow.Api.HealthChecks;

public sealed class RedisHealthCheck(IConnectionMultiplexer connection) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!connection.IsConnected)
        {
            return HealthCheckResult.Unhealthy("Redis is unavailable.");
        }

        await connection.GetDatabase().PingAsync();
        return HealthCheckResult.Healthy("Redis is reachable.");
    }
}
