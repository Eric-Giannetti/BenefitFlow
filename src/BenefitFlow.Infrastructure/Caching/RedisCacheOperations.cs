using System.Text.Json;
using System.Diagnostics;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using Polly.Timeout;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace BenefitFlow.Infrastructure.Caching;

public sealed partial class RedisCacheOperations
{
    private static readonly ActivitySource RedisActivitySource = new("BenefitFlow.Redis");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IDatabase _database;
    private readonly ResiliencePipeline _pipeline;
    private readonly ILogger<RedisCacheOperations> _logger;

    public RedisCacheOperations(
        IConnectionMultiplexer connection,
        ResiliencePipelineProvider<string> pipelineProvider,
        ILogger<RedisCacheOperations> logger)
    {
        _database = connection.GetDatabase();
        _pipeline = pipelineProvider.GetPipeline("redis");
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(RedisKey key, CancellationToken cancellationToken)
        where T : class =>
        ExecuteReadAsync(key.ToString(), async () =>
        {
            var value = await _pipeline.ExecuteAsync(
                _ => new ValueTask<RedisValue>(_database.StringGetAsync(key)),
                cancellationToken);
            return value.IsNullOrEmpty
                ? null
                : JsonSerializer.Deserialize<T>(value.ToString(), JsonOptions);
        }, cancellationToken);

    public Task SetAsync<T>(
        RedisKey key,
        T value,
        TimeSpan timeToLive,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(value);
        return ExecuteWriteAsync(key.ToString(), async () =>
        {
            await _pipeline.ExecuteAsync(
                async token =>
                {
                    await _database.StringSetAsync(
                        key,
                        JsonSerializer.Serialize(value, JsonOptions),
                        timeToLive);
                },
                cancellationToken);
        }, cancellationToken);
    }

    public Task RemoveAsync(RedisKey key, CancellationToken cancellationToken) =>
        ExecuteWriteAsync(key.ToString(), async () =>
        {
            await _pipeline.ExecuteAsync(
                async token => await _database.KeyDeleteAsync(key),
                cancellationToken);
        }, cancellationToken);

    private async Task<T?> ExecuteReadAsync<T>(
        string key,
        Func<Task<T?>> operation,
        CancellationToken cancellationToken)
        where T : class
    {
        using var activity = RedisActivitySource.StartActivity("redis.cache.get", ActivityKind.Client);
        activity?.SetTag("db.system", "redis");
        activity?.SetTag("db.operation.name", "GET");
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await operation();
        }
        catch (RedisException exception)
        {
            LogRedisFallback(_logger, exception, key);
            return null;
        }
        catch (TimeoutRejectedException exception)
        {
            LogRedisFallback(_logger, exception, key);
            return null;
        }
        catch (BrokenCircuitException exception)
        {
            LogRedisFallback(_logger, exception, key);
            return null;
        }
    }

    private async Task ExecuteWriteAsync(
        string key,
        Func<Task> operation,
        CancellationToken cancellationToken)
    {
        using var activity = RedisActivitySource.StartActivity("redis.cache.write", ActivityKind.Client);
        activity?.SetTag("db.system", "redis");
        activity?.SetTag("db.operation.name", "WRITE");
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await operation();
        }
        catch (RedisException exception)
        {
            LogRedisFallback(_logger, exception, key);
        }
        catch (TimeoutRejectedException exception)
        {
            LogRedisFallback(_logger, exception, key);
        }
        catch (BrokenCircuitException exception)
        {
            LogRedisFallback(_logger, exception, key);
        }
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Redis cache operation for key {Key} failed; PostgreSQL remains the source of truth")]
    private static partial void LogRedisFallback(ILogger logger, Exception exception, string key);
}
