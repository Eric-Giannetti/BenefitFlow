using BenefitFlow.Application;
using BenefitFlow.Infrastructure;
using BenefitFlow.Infrastructure.Observability;
using BenefitFlow.Worker.Messaging;
using BenefitFlow.Worker.Reservations;
using MassTransit;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Compact;
using Serilog.Sinks.OpenTelemetry;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using RabbitMQ.Client.Exceptions;

var builder = Host.CreateApplicationBuilder(args);
CircuitBreakerMetrics.Initialize("rabbitmq");
var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
    ?? builder.Configuration["OpenTelemetry:Endpoint"]
    ?? "http://localhost:4317";
builder.Logging.ClearProviders();
builder.Services.AddSerilog((_, loggerConfiguration) => loggerConfiguration
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithSpan()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .WriteTo.OpenTelemetry(options =>
    {
        options.Endpoint = otlpEndpoint;
        options.Protocol = OtlpProtocol.Grpc;
        options.ResourceAttributes = new Dictionary<string, object>
        {
            ["service.name"] = "BenefitFlow.Worker"
        };
    }));
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, "BenefitFlow.Worker");
builder.Services.AddResiliencePipeline("rabbitmq", pipeline =>
{
    pipeline.AddTimeout(TimeSpan.FromSeconds(5));
    pipeline.AddRetry(new RetryStrategyOptions
    {
        MaxRetryAttempts = 5,
        Delay = TimeSpan.FromMilliseconds(200),
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true,
        ShouldHandle = new PredicateBuilder()
            .Handle<MassTransitException>()
            .Handle<BrokerUnreachableException>()
    });
    pipeline.AddCircuitBreaker(new CircuitBreakerStrategyOptions
    {
        FailureRatio = 0.5,
        MinimumThroughput = 5,
        SamplingDuration = TimeSpan.FromSeconds(10),
        BreakDuration = TimeSpan.FromSeconds(10),
        ShouldHandle = new PredicateBuilder()
            .Handle<MassTransitException>()
            .Handle<BrokerUnreachableException>(),
        OnOpened = _ =>
        {
            CircuitBreakerMetrics.SetOpen("rabbitmq");
            return ValueTask.CompletedTask;
        },
        OnClosed = _ =>
        {
            CircuitBreakerMetrics.SetClosed("rabbitmq");
            return ValueTask.CompletedTask;
        },
        OnHalfOpened = _ =>
        {
            CircuitBreakerMetrics.SetHalfOpen("rabbitmq");
            return ValueTask.CompletedTask;
        }
    });
});
builder.Services.AddMassTransit(registration =>
{
    registration.AddConsumer<OutboxEventConsumer>();
    registration.UsingRabbitMq((context, rabbit) =>
    {
        var settings = builder.Configuration.GetSection("RabbitMQ");
        rabbit.Host(
            settings["Host"] ?? "localhost",
            settings["VirtualHost"] ?? "/",
            host =>
            {
                host.Username(settings["Username"] ?? "guest");
                host.Password(settings["Password"] ?? "guest");
            });
        rabbit.ReceiveEndpoint("benefitflow-domain-events", endpoint =>
        {
            endpoint.UseMessageRetry(retry =>
                retry.Exponential(
                    retryLimit: 5,
                    minInterval: TimeSpan.FromMilliseconds(200),
                    maxInterval: TimeSpan.FromSeconds(5),
                    intervalDelta: TimeSpan.FromMilliseconds(500)));
            endpoint.ConfigureConsumer<OutboxEventConsumer>(context);
        });
    });
});
builder.Services.AddHostedService<OutboxPublisher>();
builder.Services.AddHttpClient<RabbitMqQueueMetrics>();
builder.Services.AddHostedService(provider =>
    provider.GetRequiredService<RabbitMqQueueMetrics>());
builder.Services.AddHostedService<ReservationExpirationWorker>();

var host = builder.Build();
host.Run();
