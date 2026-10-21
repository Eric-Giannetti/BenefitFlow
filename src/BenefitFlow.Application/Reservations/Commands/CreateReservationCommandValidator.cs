using FluentValidation;

namespace BenefitFlow.Application.Reservations.Commands;

public sealed class CreateReservationCommandValidator : AbstractValidator<CreateReservationCommand>
{
    public CreateReservationCommandValidator()
    {
        RuleFor(command => command.BeneficiaryId).NotEmpty();
        RuleFor(command => command.BenefitType)
            .Must(Enum.IsDefined)
            .WithMessage("Benefit type is invalid.");
        RuleFor(command => command.Amount).GreaterThan(0);
        RuleFor(command => command.Currency)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Za-z]{3}$");
        RuleFor(command => command.IdempotencyKey).NotEmpty().MaximumLength(128);
    }
}
