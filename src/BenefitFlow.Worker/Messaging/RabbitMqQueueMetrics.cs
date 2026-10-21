using System.Diagnostics.Metrics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BenefitFlow.Worker.Messaging;

public sealed partial class RabbitMqQueueMetrics(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<RabbitMqQueueMetrics> logger) : BackgroundService
{
    private const string QueueName = "benefitflow-domain-events";
    private static readonly Meter Meter = new("BenefitFlow.RabbitMQ", "1.0.0");
    private static long _queueDepth;

    static RabbitMqQueueMetrics()
    {
        _ = Meter.CreateObservableGauge("rabbitmq_queue_messages", () => Interlocked.Read(ref _queueDepth));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ConfigureClient();
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await UpdateQueueDepthAsync(stoppingToken);
            }
            catch (HttpRequestException exception)
            {
                LogQueuePollFailure(logger, exception);
            }
            catch (JsonException exception)
            {
                LogQueuePollFailure(logger, exception);
            }
            catch (OperationCanceledException exception) when (!stoppingToken.IsCancellationRequested)
            {
                LogQueuePollFailure(logger, exception);
            }

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }

    private void ConfigureClient()
    {
        var managementEndpoint = configuration["RabbitMQ:ManagementEndpoint"]
            ?? throw new InvalidOperationException("RabbitMQ:ManagementEndpoint is required.");
        var username = configuration["RabbitMQ:Username"] ?? "guest";
        var password = configuration["RabbitMQ:Password"] ?? "guest";
        httpClient.BaseAddress = new Uri(managementEndpoint.TrimEnd('/') + "/");
        httpClient.Timeout = TimeSpan.FromSeconds(5);
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", credentials);
    }

    private async Task UpdateQueueDepthAsync(CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(
            $"api/queues/%2F/{QueueName}",
            cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var content = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("messages", out var messages) ||
            !messages.TryGetInt64(out var count))
        {
            throw new JsonException("RabbitMQ management response does not contain a valid messages count.");
        }

        Interlocked.Exchange(ref _queueDepth, count);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Could not read RabbitMQ queue depth; the previous metric value is retained")]
    private static partial void LogQueuePollFailure(ILogger logger, Exception exception);
}
