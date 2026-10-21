using System.Diagnostics.Metrics;

namespace BenefitFlow.Application.Observability;

public sealed class ReservationMetrics : IDisposable
{
    public const string MeterName = "BenefitFlow.Reservations";

    private readonly Meter _meter = new(MeterName, "1.0.0");
    private readonly Counter<long> _reservationsCreated;
    private readonly Counter<long> _reservationsConfirmed;
    private readonly Counter<long> _reservationsRejected;
    private readonly Counter<long> _cacheHits;
    private readonly Counter<long> _cacheMisses;
    private readonly Histogram<double> _processingDuration;
    private readonly ObservableGauge<long> _outboxPendingMessages;
    private long _pendingOutboxCount;

    public ReservationMetrics()
    {
        _reservationsCreated = _meter.CreateCounter<long>("reservations_created_total");
        _reservationsConfirmed = _meter.CreateCounter<long>("reservations_confirmed_total");
        _reservationsRejected = _meter.CreateCounter<long>("reservations_rejected_total");
        _cacheHits = _meter.CreateCounter<long>("cache_hits_total");
        _cacheMisses = _meter.CreateCounter<long>("cache_misses_total");
        _processingDuration = _meter.CreateHistogram<double>(
            "reservation_processing_duration_seconds",
            unit: "s");
        _outboxPendingMessages = _meter.CreateObservableGauge(
            "outbox_pending_messages",
            () => Interlocked.Read(ref _pendingOutboxCount));
    }

    public void ReservationCreated() => _reservationsCreated.Add(1);

    public void ReservationConfirmed() => _reservationsConfirmed.Add(1);

    public void ReservationRejected() => _reservationsRejected.Add(1);

    public void CacheHit(string cacheName) =>
        _cacheHits.Add(1, new KeyValuePair<string, object?>("cache", cacheName));

    public void CacheMiss(string cacheName) =>
        _cacheMisses.Add(1, new KeyValuePair<string, object?>("cache", cacheName));

    public void RecordProcessingDuration(double seconds) => _processingDuration.Record(seconds);

    public void SetPendingOutboxCount(long count) =>
        Interlocked.Exchange(ref _pendingOutboxCount, count);

    public void Dispose() => _meter.Dispose();
}
