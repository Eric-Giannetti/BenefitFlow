using BenefitFlow.Domain.Common;
using BenefitFlow.Domain.Enums;
using BenefitFlow.Domain.ValueObjects;

namespace BenefitFlow.Domain.Entities;

public sealed class Beneficiary : AggregateRoot
{
    private Beneficiary()
    {
        Name = string.Empty;
        Cpf = null!;
    }

    private Beneficiary(Guid id, string name, Cpf cpf)
    {
        Id = id;
        Name = name;
        Cpf = cpf;
        Status = BeneficiaryStatus.Active;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public Cpf Cpf { get; private set; }

    public BeneficiaryStatus Status { get; private set; }

    public static Result<Beneficiary> Create(Guid id, string? name, Cpf? cpf)
    {
        if (id == Guid.Empty || cpf is null)
        {
            return Result.Failure<Beneficiary>(DomainErrors.InvalidBeneficiaryIdentity);
        }

        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200)
        {
            return Result.Failure<Beneficiary>(DomainErrors.InvalidBeneficiaryName);
        }

        return Result.Success(new Beneficiary(id, name.Trim(), cpf));
    }

    public Result Deactivate()
    {
        if (Status == BeneficiaryStatus.Inactive)
        {
            return Result.Failure(DomainErrors.BeneficiaryAlreadyInactive);
        }

        Status = BeneficiaryStatus.Inactive;
        return Result.Success();
    }

    public Result Activate()
    {
        if (Status == BeneficiaryStatus.Active)
        {
            return Result.Failure(DomainErrors.BeneficiaryAlreadyActive);
        }

        Status = BeneficiaryStatus.Active;
        return Result.Success();
    }
}
