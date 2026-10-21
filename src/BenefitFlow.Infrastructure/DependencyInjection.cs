using BenefitFlow.Application.Abstractions.Caching;
using BenefitFlow.Application.Abstractions.Configuration;
using BenefitFlow.Application.Abstractions.External;
using BenefitFlow.Application.Abstractions.Persistence;
using BenefitFlow.Infrastructure.Caching;
using BenefitFlow.Infrastructure.Configuration;
using BenefitFlow.Infrastructure.External;
using BenefitFlow.Infrastructure.Persistence;
using BenefitFlow.Infrastructure.Persistence.Repositories;
using BenefitFlow.Infrastructure.Observability;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace BenefitFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        var postgresConnection = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required.");
        var redisConnection = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("ConnectionStrings:Redis is required.");

        services.AddDbContext<BenefitFlowDbContext>(options =>
            options.UseNpgsql(postgresConnection));
        services.AddScoped<IUnitOfWork>(provider =>
            provider.GetRequiredService<BenefitFlowDbContext>());
        services.AddScoped<IBeneficiaryRepository, BeneficiaryRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IBenefitBalanceRepository, BenefitBalanceRepository>();

        var redis = ConnectionMultiplexer.Connect(redisConnection, options =>
            options.AbortOnConnectFail = false);
        services.AddSingleton<IConnectionMultiplexer>(redis);
        services.AddBenefitFlowObservability(
            configuration,
            serviceName);
        services.AddBenefitFlowResilience();
        services.AddSingleton<RedisCacheOperations>();
        services.AddSingleton<IReservationCache, RedisReservationCache>();
        services.AddSingleton<IBenefitBalanceCache, RedisBenefitBalanceCache>();
        services.AddSingleton<IIdempotencyCache, RedisIdempotencyCache>();

        services.AddOptions<ReservationOptions>()
            .Bind(configuration.GetSection("Reservations"))
            .Validate(options =>
                    options.PendingExpirationMinutes > 0 &&
                    options.ReadCacheTimeToLiveSeconds > 0 &&
                    options.IdempotencyCacheTimeToLiveHours > 0,
                "Reservation expiration and cache TTL values must be greater than zero.")
            .ValidateOnStart();
        services.AddSingleton<IReservationPolicy, ReservationPolicy>();
        services.AddOptions<EligibilityOptions>()
            .Bind(configuration.GetSection("Eligibility"))
            .Validate(options => options.FailFirstRequests >= 0, "Failure count cannot be negative.")
            .ValidateOnStart();
        services.AddTransient<SimulatedEligibilityHandler>();
        services.AddHttpClient<IEligibilityService, EligibilityHttpClient>(client =>
        {
            client.BaseAddress = new Uri("http://eligibility-simulator/");
            client.Timeout = Timeout.InfiniteTimeSpan;
        }).AddHttpMessageHandler<SimulatedEligibilityHandler>();

        return services;
    }
}
