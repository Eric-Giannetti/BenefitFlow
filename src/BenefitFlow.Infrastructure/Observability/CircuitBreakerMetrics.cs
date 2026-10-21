using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace BenefitFlow.Infrastructure.Observability;

public static class CircuitBreakerMetrics
{
    public const string MeterName = "BenefitFlow.Resilience";

    private static readonly ConcurrentDictionary<string, long> States = new(StringComparer.Ordinal);
    private static readonly Meter Meter = new(MeterName, "1.0.0");

    static CircuitBreakerMetrics()
    {
        _ = Meter.CreateObservableGauge("circuit_breaker_state", ObserveStates);
    }

    public static void Initialize(string pipelineName) => States.TryAdd(pipelineName, 0);

    public static void SetOpen(string pipelineName) => States[pipelineName] = 1;

    public static void SetClosed(string pipelineName) => States[pipelineName] = 0;

    public static void SetHalfOpen(string pipelineName) => States[pipelineName] = 2;

    private static IEnumerable<Measurement<long>> ObserveStates()
    {
        foreach (var (pipelineName, state) in States)
        {
            yield return new Measurement<long>(
                state,
                new KeyValuePair<string, object?>("pipeline", pipelineName));
        }
    }
}
