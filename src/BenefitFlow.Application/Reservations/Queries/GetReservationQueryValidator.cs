using FluentValidation;

namespace BenefitFlow.Application.Reservations.Queries;

public sealed class GetReservationQueryValidator : AbstractValidator<GetReservationQuery>
{
    public GetReservationQueryValidator()
    {
        RuleFor(query => query.ReservationId).NotEmpty();
    }
}
