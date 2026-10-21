using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

namespace BenefitFlow.IntegrationTests;

public sealed class BenefitFlowContainers : IAsyncLifetime
{
    public const string RunEnvironmentVariable = "BENEFITFLOW_RUN_INTEGRATION_TESTS";

    public PostgreSqlContainer Postgres { get; } =
        new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();

    public RedisContainer Redis { get; } =
        new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();

    public RabbitMqContainer RabbitMq { get; } =
        new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management-alpine")
            .WithUsername("benefitflow")
            .WithPassword("benefitflow-dev")
            .Build();

    public async Task InitializeAsync()
    {
        if (!IsEnabled)
        {
            return;
        }

        await Task.WhenAll(
            Postgres.StartAsync(),
            Redis.StartAsync(),
            RabbitMq.StartAsync());
    }

    public async Task DisposeAsync()
    {
        await Task.WhenAll(
            Postgres.DisposeAsync().AsTask(),
            Redis.DisposeAsync().AsTask(),
            RabbitMq.DisposeAsync().AsTask());
    }

    public static bool IsEnabled =>
        string.Equals(
            Environment.GetEnvironmentVariable(RunEnvironmentVariable),
            "true",
            StringComparison.OrdinalIgnoreCase);
}

public sealed class DockerFactAttribute : FactAttribute
{
    public DockerFactAttribute()
    {
        if (!BenefitFlowContainers.IsEnabled)
        {
            Skip = $"Set {BenefitFlowContainers.RunEnvironmentVariable}=true to run Docker-backed integration tests.";
        }
    }
}
