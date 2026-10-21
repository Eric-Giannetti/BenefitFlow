using BenefitFlow.Api.Endpoints;
using BenefitFlow.Api.Middleware;
using BenefitFlow.Api.HealthChecks;
using BenefitFlow.Api.OpenApi;
using BenefitFlow.Api.Startup;
using BenefitFlow.Application;
using BenefitFlow.Infrastructure;
using BenefitFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json.Serialization;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Compact;
using Serilog.Sinks.OpenTelemetry;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
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
            ["service.name"] = "BenefitFlow.Api"
        };
    }));
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, "BenefitFlow.Api");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.SchemaFilter<JsonStringEnumSchemaFilter>());
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("postgres", tags: ["ready"])
    .AddCheck<RedisHealthCheck>("redis", tags: ["ready"])
    .AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: ["ready"]);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("reservation-create", limiter =>
    {
        limiter.PermitLimit = 100;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });
});

var app = builder.Build();

app.UseProblemDetailsExceptionHandler();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapReservationEndpoints();
app.MapBeneficiaryEndpoints();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = _ => true });

await DatabaseStartup.InitializeAsync(
    app.Services,
    app.Configuration,
    app.Lifetime.ApplicationStopping);

app.Run();

public partial class Program;
