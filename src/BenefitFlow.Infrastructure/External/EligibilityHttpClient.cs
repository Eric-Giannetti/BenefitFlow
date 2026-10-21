using System.Net.Http.Json;
using BenefitFlow.Application.Abstractions.External;
using Polly;
using Polly.Registry;

namespace BenefitFlow.Infrastructure.External;

public sealed class EligibilityHttpClient(
    HttpClient httpClient,
    ResiliencePipelineProvider<string> pipelineProvider) : IEligibilityService
{
    private readonly ResiliencePipeline _pipeline = pipelineProvider.GetPipeline("eligibility");

    public async Task<EligibilityDecision> CheckAsync(
        Guid beneficiaryId,
        CancellationToken cancellationToken = default)
    {
        return await _pipeline.ExecuteAsync(
            async token => await httpClient.GetFromJsonAsync<EligibilityDecision>(
                $"beneficiaries/{beneficiaryId:D}",
                token) ?? throw new InvalidDataException("Eligibility service returned an empty response."),
            cancellationToken);
    }
}
