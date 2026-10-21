using BenefitFlow.Domain.ValueObjects;
using FluentAssertions;

namespace BenefitFlow.Domain.UnitTests.ValueObjects;

public sealed class CpfTests
{
    [Fact]
    public void Create_with_valid_formatted_cpf_normalizes_digits()
    {
        var result = Cpf.Create("529.982.247-25");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be("52998224725");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("111.111.111-11")]
    [InlineData("529.982.247-24")]
    [InlineData("5.299.822.472-5")]
    [InlineData("5299822472")]
    [InlineData("529 982 247 25")]
    public void Create_rejects_invalid_cpf(string? value)
    {
        var result = Cpf.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("beneficiary.invalid_cpf");
    }
}
