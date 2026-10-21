using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BenefitFlow.Api;
using BenefitFlow.Api.Models;
using BenefitFlow.Application;
using BenefitFlow.Application.DTOs;
using BenefitFlow.Domain.Entities;
using BenefitFlow.Domain.Enums;
using BenefitFlow.Domain.ValueObjects;
using BenefitFlow.Infrastructure.Persistence;
using BenefitFlow.Infrastructure;
using BenefitFlow.Worker.Messaging;
using FluentAssertions;
using MassTransit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Resilience;
using Npgsql;
using Polly;
using System.Globalization;

namespace BenefitFlow.IntegrationTests;

public sealed class ReservationFlowIntegrationTests(BenefitFlowContainers containers)
    : IClassFixture<BenefitFlowContainers>
{
    private static readonly Guid BeneficiaryId = Guid.Parse("f87d30ea-8806-4e18-b292-0e03a52178dd");
    private const string IdempotencyKey = "integration-flow-0001";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [DockerFact]
    public async Task Api_outbox_worker_idempotency_and_optimistic_concurrency_work_together()
    {
        var settings = CreateSettings();
        using var environment = new EnvironmentOverride(settings);
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
                builder.ConfigureAppConfiguration((_, configuration) =>
                    configuration.AddInMemoryCollection(settings)));
        using var client = factory.CreateClient();

        using var worker = CreateWorkerHost(settings);
        await worker.StartAsync();

        var accepted = await PostReservationAsync(
            client,
            new CreateReservationRequest(BeneficiaryId, BenefitType.Consultation, 25m),
            IdempotencyKey);
        accepted.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var created = await accepted.Content.ReadFromJsonAsync<ReservationResponse>(JsonOptions);
        created.Should().NotBeNull();

        var duplicate = await PostReservationAsync(
            client,
            new CreateReservationRequest(BeneficiaryId, BenefitType.Consultation, 25m),
            IdempotencyKey);
        duplicate.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var duplicateResult = await duplicate.Content.ReadFromJsonAsync<ReservationResponse>(JsonOptions);
        duplicateResult!.Id.Should().Be(created!.Id);

        var processedReservation = await WaitForConfirmationAsync(client, created.Id);
        processedReservation.Status.Should().Be(ReservationStatus.Confirmed);

        var balance = await client.GetFromJsonAsync<BenefitBalanceResponse>(
            $"/api/v1/beneficiaries/{BeneficiaryId}/balance?benefitType=Consultation",
            JsonOptions);
        balance!.Amount.Should().Be(475m);

        await using (var verificationScope = factory.Services.CreateAsyncScope())
        {
            var dbContext = verificationScope.ServiceProvider.GetRequiredService<BenefitFlowDbContext>();
            (await dbContext.OutboxMessages.AnyAsync(message =>
                message.ProcessedAt != null && message.TraceParent != null)).Should().BeTrue();
            (await dbContext.ProcessedMessages.AnyAsync()).Should().BeTrue();
        }

        await AssertOptimisticConcurrencyAsync(containers.Postgres.GetConnectionString());
        await worker.StopAsync();
    }

    private static async Task<ReservationResponse> WaitForConfirmationAsync(
        HttpClient client,
        Guid reservationId)
    {
        for (var attempt = 0; attempt < 60; attempt++)
        {
            var response = await client.GetAsync($"/api/v1/reservations/{reservationId}");
            if (response.IsSuccessStatusCode)
            {
                var reservation = await response.Content.ReadFromJsonAsync<ReservationResponse>(JsonOptions);
                if (reservation?.Status == ReservationStatus.Confirmed)
                {
                    return reservation;
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        throw new TimeoutException($"Reservation {reservationId} was not confirmed within 30 seconds.");
    }

    private static async Task<HttpResponseMessage> PostReservationAsync(
        HttpClient client,
        CreateReservationRequest requestBody,
        string idempotencyKey)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/reservations")
        {
            Content = JsonContent.Create(requestBody)
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);
        return await client.SendAsync(request);
    }

    private static IHost CreateWorkerHost(IDictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        return Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddApplication();
                services.AddInfrastructure(configuration, "BenefitFlow.TestWorker");
                services.AddResiliencePipeline("rabbitmq", pipeline =>
                    pipeline.AddTimeout(TimeSpan.FromSeconds(5)));
                services.AddMassTransit(registration =>
                {
                    registration.AddConsumer<OutboxEventConsumer>();
                    registration.UsingRabbitMq((context, rabbit) =>
                    {
                        var host = configuration["RabbitMQ:Host"]!;
                        var port = ushort.Parse(
                            configuration["RabbitMQ:Port"]!,
                            CultureInfo.InvariantCulture);
                        var username = configuration["RabbitMQ:Username"]!;
                        var password = configuration["RabbitMQ:Password"]!;
                        rabbit.Host(host, port, "/", hostConfiguration =>
                        {
                            hostConfiguration.Username(username);
                            hostConfiguration.Password(password);
                        });
                        rabbit.ReceiveEndpoint("benefitflow-domain-events", endpoint =>
                            endpoint.ConfigureConsumer<OutboxEventConsumer>(context));
                    });
                });
                services.AddHostedService<OutboxPublisher>();
            })
            .Build();
    }

    private Dictionary<string, string?> CreateSettings() => new()
    {
        ["ConnectionStrings:Postgres"] = new NpgsqlConnectionStringBuilder(
            containers.Postgres.GetConnectionString())
        {
            Host = containers.Postgres.Hostname,
            Port = containers.Postgres.GetMappedPublicPort(5432)
        }.ConnectionString,
        ["ConnectionStrings:Redis"] =
            $"{containers.Redis.Hostname}:{containers.Redis.GetMappedPublicPort(6379)}",
        ["RabbitMQ:Host"] = containers.RabbitMq.Hostname,
        ["RabbitMQ:Port"] = containers.RabbitMq
            .GetMappedPublicPort(5672)
            .ToString(CultureInfo.InvariantCulture),
        ["RabbitMQ:Username"] = "benefitflow",
        ["RabbitMQ:Password"] = "benefitflow-dev",
        ["Database:ApplyMigrations"] = "true",
        ["Database:SeedDemoData"] = "true",
        ["OpenTelemetry:Endpoint"] = "http://localhost:4317",
        ["Reservations:PendingExpirationMinutes"] = "5",
        ["Reservations:ReadCacheTimeToLiveSeconds"] = "30",
        ["Reservations:IdempotencyCacheTimeToLiveHours"] = "24"
    };

    private static async Task AssertOptimisticConcurrencyAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<BenefitFlowDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var firstContext = new BenefitFlowDbContext(options);
        await using var secondContext = new BenefitFlowDbContext(options);
        var firstBalance = await firstContext.BenefitBalances.SingleAsync(balance =>
            balance.BeneficiaryId == BeneficiaryId &&
            balance.BenefitType == BenefitType.Consultation);
        var secondBalance = await secondContext.BenefitBalances.SingleAsync(balance =>
            balance.BeneficiaryId == BeneficiaryId &&
            balance.BenefitType == BenefitType.Consultation);
        var oneUnit = Money.Create(1m).Value!;

        firstBalance.Reserve(oneUnit).IsSuccess.Should().BeTrue();
        secondBalance.Reserve(oneUnit).IsSuccess.Should().BeTrue();
        await firstContext.SaveChangesAsync();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            secondContext.SaveChangesAsync());
    }

    private sealed class EnvironmentOverride : IDisposable
    {
        private readonly Dictionary<string, string?> _originalValues = new(StringComparer.Ordinal);

        public EnvironmentOverride(IDictionary<string, string?> settings)
        {
            foreach (var (key, value) in settings)
            {
                var name = key.Replace(":", "__", StringComparison.Ordinal);
                _originalValues[name] = Environment.GetEnvironmentVariable(name);
                Environment.SetEnvironmentVariable(name, value);
            }
        }

        public void Dispose()
        {
            foreach (var (name, value) in _originalValues)
            {
                Environment.SetEnvironmentVariable(name, value);
            }
        }
    }
}
