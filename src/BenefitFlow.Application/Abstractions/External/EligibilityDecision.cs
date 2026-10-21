namespace BenefitFlow.Application.Abstractions.External;

public sealed record EligibilityDecision(bool IsEligible, string? Reason);
