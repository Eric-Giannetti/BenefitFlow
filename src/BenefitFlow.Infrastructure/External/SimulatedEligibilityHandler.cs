using System.Net;
using System.Net.Http.Json;
using BenefitFlow.Application.Abstractions.External;
using BenefitFlow.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace BenefitFlow.Infrastructure.External;

public sealed class SimulatedEligibilityHandler : DelegatingHandler
{
    private int _remainingFailures;

    public SimulatedEligibilityHandler(IOptions<EligibilityOptions> options)
    {
        _remainingFailures = options.Value.FailFirstRequests;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Interlocked.Decrement(ref _remainingFailures) >= 0)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new EligibilityDecision(true, null))
        });
    }
}
