using BenefitFlow.Application.Abstractions.Persistence;
using BenefitFlow.Application.Abstractions.Caching;
using BenefitFlow.Application.Observability;
using BenefitFlow.Application.Abstractions.External;
using BenefitFlow.Application.Reservations.Commands;
using BenefitFlow.Domain.Entities;
using BenefitFlow.Domain.Enums;
using BenefitFlow.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;

namespace BenefitFlow.Application.UnitTests.Reservations;

public sealed class ProcessReservationCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_reserves_balance_and_confirms_pending_reservation()
    {
        var beneficiary = CreateBeneficiary();
        var reservation = CreateReservation(beneficiary.Id, Now.AddMinutes(5));
        var balance = CreateBalance(beneficiary.Id, 100m);
        var (handler, beneficiaryRepository, reservationRepository, balanceRepository, unitOfWork) =
            CreateHandler(reservation, beneficiary, balance);

        var result = await handler.Handle(
            new ProcessReservationCommand(reservation.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(ReservationStatus.Confirmed);
        balance.AvailableAmount.Amount.Should().Be(75m);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await beneficiaryRepository.Received(1).GetByIdAsync(
            beneficiary.Id,
            Arg.Any<CancellationToken>());
        await reservationRepository.Received(1).GetByIdAsync(
            reservation.Id,
            Arg.Any<CancellationToken>());
        await balanceRepository.Received(1).GetByBeneficiaryAndTypeAsync(
            beneficiary.Id,
            BenefitType.Consultation,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_rejects_reservation_when_balance_is_insufficient()
    {
        var beneficiary = CreateBeneficiary();
        var reservation = CreateReservation(beneficiary.Id, Now.AddMinutes(5));
        var balance = CreateBalance(beneficiary.Id, 10m);
        var (handler, _, _, _, unitOfWork) = CreateHandler(reservation, beneficiary, balance);

        var result = await handler.Handle(
            new ProcessReservationCommand(reservation.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(ReservationStatus.Rejected);
        result.Value.RejectionReason.Should().NotBeNullOrWhiteSpace();
        balance.AvailableAmount.Amount.Should().Be(10m);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_expires_a_pending_reservation_after_its_deadline()
    {
        var beneficiary = CreateBeneficiary();
        var reservation = CreateReservation(beneficiary.Id, Now);
        var balanceRepository = Substitute.For<IBenefitBalanceRepository>();
        var (handler, _, _, _, unitOfWork) = CreateHandler(
            reservation,
            beneficiary,
            balance: null,
            balanceRepository);

        var result = await handler.Handle(
            new ProcessReservationCommand(reservation.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(ReservationStatus.Expired);
        await balanceRepository.DidNotReceive().GetByBeneficiaryAndTypeAsync(
            Arg.Any<Guid>(),
            Arg.Any<BenefitType>(),
            Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_does_not_process_terminal_reservation_again()
    {
        var beneficiary = CreateBeneficiary();
        var reservation = CreateReservation(beneficiary.Id, Now.AddMinutes(5));
        reservation.Reject("Already rejected", Now.AddMinutes(-1)).IsSuccess.Should().BeTrue();
        var balanceRepository = Substitute.For<IBenefitBalanceRepository>();
        var (handler, _, _, _, unitOfWork) = CreateHandler(
            reservation,
            beneficiary,
            balance: null,
            balanceRepository);

        var result = await handler.Handle(
            new ProcessReservationCommand(reservation.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(ReservationStatus.Rejected);
        await balanceRepository.DidNotReceive().GetByBeneficiaryAndTypeAsync(
            Arg.Any<Guid>(),
            Arg.Any<BenefitType>(),
            Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static (
        ProcessReservationCommandHandler Handler,
        IBeneficiaryRepository BeneficiaryRepository,
        IReservationRepository ReservationRepository,
        IBenefitBalanceRepository BalanceRepository,
        IUnitOfWork UnitOfWork) CreateHandler(
        BenefitReservation reservation,
        Beneficiary beneficiary,
        BenefitBalance? balance,
        IBenefitBalanceRepository? balanceRepository = null)
    {
        var beneficiaryRepository = Substitute.For<IBeneficiaryRepository>();
        var reservationRepository = Substitute.For<IReservationRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        balanceRepository ??= Substitute.For<IBenefitBalanceRepository>();
        var eligibilityService = Substitute.For<IEligibilityService>();
        beneficiaryRepository.GetByIdAsync(beneficiary.Id, Arg.Any<CancellationToken>())
            .Returns(beneficiary);
        reservationRepository.GetByIdAsync(reservation.Id, Arg.Any<CancellationToken>())
            .Returns(reservation);
        balanceRepository.GetByBeneficiaryAndTypeAsync(
                beneficiary.Id,
                BenefitType.Consultation,
                Arg.Any<CancellationToken>())
            .Returns(balance);
        eligibilityService.CheckAsync(beneficiary.Id, Arg.Any<CancellationToken>())
            .Returns(new EligibilityDecision(true, null));
        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(Now);

        return (
            new ProcessReservationCommandHandler(
                reservationRepository,
                beneficiaryRepository,
                balanceRepository,
                eligibilityService,
                unitOfWork,
                Substitute.For<IReservationCache>(),
                Substitute.For<IBenefitBalanceCache>(),
                new ReservationMetrics(),
                timeProvider),
            beneficiaryRepository,
            reservationRepository,
            balanceRepository,
            unitOfWork);
    }

    private static Beneficiary CreateBeneficiary() =>
        Beneficiary.Create(
            Guid.NewGuid(),
            "Ana Silva",
            Cpf.Create("52998224725").Value!).Value!;

    private static BenefitReservation CreateReservation(Guid beneficiaryId, DateTimeOffset expiresAt) =>
        BenefitReservation.Create(
            Guid.NewGuid(),
            beneficiaryId,
            BenefitType.Consultation,
            Money.Create(25m).Value!,
            Now.AddMinutes(-1),
            expiresAt,
            Guid.NewGuid().ToString("N")).Value!;

    private static BenefitBalance CreateBalance(Guid beneficiaryId, decimal amount) =>
        BenefitBalance.Open(
            Guid.NewGuid(),
            beneficiaryId,
            BenefitType.Consultation,
            Money.Create(amount).Value!).Value!;
}
