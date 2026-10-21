namespace BenefitFlow.Application.Abstractions.External;

public interface IEligibilityService
{
    Task<EligibilityDecision> CheckAsync(
        Guid beneficiaryId,
        CancellationToken cancellationToken = default);
}
