using BenefitFlow.Domain.Entities;
using BenefitFlow.Domain.Enums;
using BenefitFlow.Domain.ValueObjects;
using FluentAssertions;

namespace BenefitFlow.Domain.UnitTests.Entities;

public sealed class BeneficiaryTests
{
    [Fact]
    public void Create_trims_name_and_starts_active()
    {
        var beneficiary = Beneficiary.Create(
            Guid.NewGuid(),
            "  Ana Silva  ",
            Cpf.Create("52998224725").Value!);

        beneficiary.IsSuccess.Should().BeTrue();
        beneficiary.Value!.Name.Should().Be("Ana Silva");
        beneficiary.Value.Status.Should().Be(BeneficiaryStatus.Active);
    }

    [Fact]
    public void Deactivate_and_activate_enforce_state_transitions()
    {
        var beneficiary = Beneficiary.Create(
            Guid.NewGuid(),
            "Ana Silva",
            Cpf.Create("52998224725").Value!).Value!;

        beneficiary.Deactivate().IsSuccess.Should().BeTrue();
        beneficiary.Deactivate().Error!.Code.Should().Be("beneficiary.already_inactive");
        beneficiary.Activate().IsSuccess.Should().BeTrue();
        beneficiary.Activate().Error!.Code.Should().Be("beneficiary.already_active");
    }

    [Fact]
    public void Create_rejects_invalid_identity()
    {
        var result = Beneficiary.Create(Guid.Empty, " ", Cpf.Create("52998224725").Value!);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("beneficiary.invalid_identity");
    }
}
