using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BenefitFlow.Api.HealthChecks;

public sealed class RabbitMqHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var settings = configuration.GetSection("RabbitMQ");
        var host = settings["Host"] ?? "localhost";
        var port = int.TryParse(settings["Port"], out var configuredPort)
            ? configuredPort
            : 5672;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(3));
        using var client = new TcpClient();

        try
        {
            await client.ConnectAsync(host, port, timeout.Token);
            return HealthCheckResult.Healthy("RabbitMQ is reachable.");
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ connection timed out.", exception);
        }
        catch (SocketException exception)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ is unavailable.", exception);
        }
    }
}
