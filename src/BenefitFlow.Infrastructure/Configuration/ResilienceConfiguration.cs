using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using BenefitFlow.Infrastructure.Observability;

namespace BenefitFlow.Infrastructure.Configuration;

public static class ResilienceConfiguration
{
    public static IServiceCollection AddBenefitFlowResilience(this IServiceCollection services)
    {
        CircuitBreakerMetrics.Initialize("redis");
        CircuitBreakerMetrics.Initialize("eligibility");
        services.AddResiliencePipeline("redis", ConfigureRedisPipeline);
        services.AddResiliencePipeline("eligibility", ConfigureEligibilityPipeline);
        return services;
    }

    private static void ConfigureRedisPipeline(ResiliencePipelineBuilder builder)
    {
        var shouldHandle = new PredicateBuilder().Handle<RedisException>();
        builder.AddTimeout(TimeSpan.FromSeconds(2));
        builder.AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(200),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = shouldHandle
        });
        builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            MinimumThroughput = 5,
            SamplingDuration = TimeSpan.FromSeconds(10),
            BreakDuration = TimeSpan.FromSeconds(10),
            ShouldHandle = new PredicateBuilder().Handle<RedisException>(),
            OnOpened = _ =>
            {
                CircuitBreakerMetrics.SetOpen("redis");
                return ValueTask.CompletedTask;
            },
            OnClosed = _ =>
            {
                CircuitBreakerMetrics.SetClosed("redis");
                return ValueTask.CompletedTask;
            },
            OnHalfOpened = _ =>
            {
                CircuitBreakerMetrics.SetHalfOpen("redis");
                return ValueTask.CompletedTask;
            }
        });
    }

    private static void ConfigureEligibilityPipeline(ResiliencePipelineBuilder builder)
    {
        builder.AddTimeout(TimeSpan.FromSeconds(3));
        builder.AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(200),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>(
                exception => exception.StatusCode is null ||
                    (int)exception.StatusCode >= 500)
        });
        builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            MinimumThroughput = 5,
            SamplingDuration = TimeSpan.FromSeconds(10),
            BreakDuration = TimeSpan.FromSeconds(10),
            ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>(
                exception => exception.StatusCode is null ||
                    (int)exception.StatusCode >= 500),
            OnOpened = _ =>
            {
                CircuitBreakerMetrics.SetOpen("eligibility");
                return ValueTask.CompletedTask;
            },
            OnClosed = _ =>
            {
                CircuitBreakerMetrics.SetClosed("eligibility");
                return ValueTask.CompletedTask;
            },
            OnHalfOpened = _ =>
            {
                CircuitBreakerMetrics.SetHalfOpen("eligibility");
                return ValueTask.CompletedTask;
            }
        });
    }
}
