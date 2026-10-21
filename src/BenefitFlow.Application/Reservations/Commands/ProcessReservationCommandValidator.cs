using FluentValidation;

namespace BenefitFlow.Application.Reservations.Commands;

public sealed class ProcessReservationCommandValidator : AbstractValidator<ProcessReservationCommand>
{
    public ProcessReservationCommandValidator()
    {
        RuleFor(command => command.ReservationId).NotEmpty();
    }
}
