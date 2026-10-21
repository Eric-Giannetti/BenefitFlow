using BenefitFlow.Application.Abstractions.Configuration;
using BenefitFlow.Application.Abstractions.Caching;
using BenefitFlow.Application.Observability;
using BenefitFlow.Application.Abstractions.Persistence;
using BenefitFlow.Application.Common;
using BenefitFlow.Application.DTOs;
using BenefitFlow.Application.Mapping;
using BenefitFlow.Application.Reservations.Commands;
using BenefitFlow.Domain.Entities;
using BenefitFlow.Domain.Enums;
using BenefitFlow.Domain.ValueObjects;
using FluentAssertions;
using Mapster;
using NSubstitute;

namespace BenefitFlow.Application.UnitTests.Reservations;

public sealed class CreateReservationCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_creates_reservation_and_saves_once()
    {
        var beneficiary = CreateBeneficiary();
        var reservationRepository = Substitute.For<IReservationRepository>();
        var beneficiaryRepository = Substitute.For<IBeneficiaryRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var mappingConfiguration = CreateMappingConfiguration();
        beneficiaryRepository.GetByIdAsync(beneficiary.Id, Arg.Any<CancellationToken>())
            .Returns(beneficiary);
        reservationRepository.GetByIdempotencyKeyAsync("key-1", Arg.Any<CancellationToken>())
            .Returns((BenefitReservation?)null);
        var handler = CreateHandler(
            beneficiaryRepository,
            reservationRepository,
            unitOfWork,
            mappingConfiguration);
        var command = new CreateReservationCommand(
            beneficiary.Id,
            BenefitType.Consultation,
            75m,
            "BRL",
            "key-1");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(ReservationStatus.Pending);
        result.Value.Amount.Should().Be(75m);
        result.Value.ExpiresAt.Should().Be(Now.AddMinutes(5));
        reservationRepository.Received(1).Add(Arg.Is<BenefitReservation>(
            reservation => reservation.IdempotencyKey == "key-1"));
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_returns_existing_reservation_for_matching_idempotency_key()
    {
        var beneficiary = CreateBeneficiary();
        var existing = CreateReservation(beneficiary.Id, 75m, "key-1");
        var reservationRepository = Substitute.For<IReservationRepository>();
        var beneficiaryRepository = Substitute.For<IBeneficiaryRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        reservationRepository.GetByIdempotencyKeyAsync("key-1", Arg.Any<CancellationToken>())
            .Returns(existing);
        var handler = CreateHandler(
            beneficiaryRepository,
            reservationRepository,
            unitOfWork,
            CreateMappingConfiguration());

        var result = await handler.Handle(
            new CreateReservationCommand(beneficiary.Id, BenefitType.Consultation, 75m, "brl", "key-1"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(existing.Id);
        await beneficiaryRepository.DidNotReceive()
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_rejects_reuse_of_key_with_different_payload()
    {
        var beneficiaryId = Guid.NewGuid();
        var existing = CreateReservation(beneficiaryId, 75m, "key-1");
        var reservationRepository = Substitute.For<IReservationRepository>();
        reservationRepository.GetByIdempotencyKeyAsync("key-1", Arg.Any<CancellationToken>())
            .Returns(existing);
        var handler = CreateHandler(
            Substitute.For<IBeneficiaryRepository>(),
            reservationRepository,
            Substitute.For<IUnitOfWork>(),
            CreateMappingConfiguration());

        var result = await handler.Handle(
            new CreateReservationCommand(beneficiaryId, BenefitType.Consultation, 90m, "BRL", "key-1"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Should().Be(ApplicationErrors.IdempotencyKeyConflict);
    }

    private static CreateReservationCommandHandler CreateHandler(
        IBeneficiaryRepository beneficiaryRepository,
        IReservationRepository reservationRepository,
        IUnitOfWork unitOfWork,
        TypeAdapterConfig mappingConfiguration)
    {
        var policy = Substitute.For<IReservationPolicy>();
        policy.PendingExpiration.Returns(TimeSpan.FromMinutes(5));
        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(Now);

        return new CreateReservationCommandHandler(
            beneficiaryRepository,
            reservationRepository,
            Substitute.For<IIdempotencyCache>(),
            policy,
            unitOfWork,
            mappingConfiguration,
            new ReservationMetrics(),
            timeProvider);
    }

    private static TypeAdapterConfig CreateMappingConfiguration()
    {
        var configuration = new TypeAdapterConfig();
        ApplicationMappingConfiguration.Configure(configuration);
        return configuration;
    }

    private static Beneficiary CreateBeneficiary() =>
        Beneficiary.Create(
            Guid.NewGuid(),
            "Ana Silva",
            Cpf.Create("52998224725").Value!).Value!;

    private static BenefitReservation CreateReservation(Guid beneficiaryId, decimal amount, string key) =>
        BenefitReservation.Create(
            Guid.NewGuid(),
            beneficiaryId,
            BenefitType.Consultation,
            Money.Create(amount).Value!,
            Now,
            Now.AddMinutes(5),
            key).Value!;
}
