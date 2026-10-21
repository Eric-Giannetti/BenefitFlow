using BenefitFlow.Application.Observability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace BenefitFlow.Infrastructure.Observability;

public static class BenefitFlowObservabilityExtensions
{
    public static IServiceCollection AddBenefitFlowObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        var endpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
            ?? configuration["OpenTelemetry:Endpoint"]
            ?? "http://localhost:4317";
        var otlpEndpoint = new Uri(endpoint);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddSource("Npgsql")
                .AddSource("MassTransit")
                .AddSource("BenefitFlow.Outbox")
                .AddSource("BenefitFlow.Redis")
                .AddOtlpExporter(exporter => exporter.Endpoint = otlpEndpoint))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(ReservationMetrics.MeterName)
                .AddMeter(CircuitBreakerMetrics.MeterName)
                .AddMeter("BenefitFlow.RabbitMQ")
                .AddOtlpExporter(exporter => exporter.Endpoint = otlpEndpoint));

        return services;
    }
}
