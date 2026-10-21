using BenefitFlow.Application.Reservations.Commands;
using BenefitFlow.Domain.Enums;
using FluentAssertions;

namespace BenefitFlow.Application.UnitTests.Reservations;

public sealed class CreateReservationCommandValidatorTests
{
    private readonly CreateReservationCommandValidator _validator = new();

    [Fact]
    public void Validate_accepts_a_well_formed_reservation()
    {
        var command = new CreateReservationCommand(
            Guid.NewGuid(),
            BenefitType.Consultation,
            75m,
            "brl",
            "request-123");

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_rejects_missing_ids_invalid_type_amount_currency_and_key()
    {
        var command = new CreateReservationCommand(
            Guid.Empty,
            (BenefitType)99,
            0m,
            "R$",
            string.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(5);
    }
}
