using FluentValidation;

namespace BenefitFlow.Application.Reservations.Commands;

public sealed class CancelReservationCommandValidator : AbstractValidator<CancelReservationCommand>
{
    public CancelReservationCommandValidator()
    {
        RuleFor(command => command.ReservationId).NotEmpty();
    }
}
