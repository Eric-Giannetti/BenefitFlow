using FluentValidation;

namespace BenefitFlow.Application.Balances.Queries;

public sealed class GetBenefitBalanceQueryValidator : AbstractValidator<GetBenefitBalanceQuery>
{
    public GetBenefitBalanceQueryValidator()
    {
        RuleFor(query => query.BeneficiaryId).NotEmpty();
        RuleFor(query => query.BenefitType)
            .Must(Enum.IsDefined)
            .WithMessage("Benefit type is invalid.");
    }
}
