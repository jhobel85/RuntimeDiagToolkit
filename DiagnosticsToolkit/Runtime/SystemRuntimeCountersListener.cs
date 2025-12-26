using System.Collections.Concurrent;
using System.Diagnostics.Tracing;

namespace DiagnosticsToolkit.Runtime;

/// <summary>
/// Listens to the "System.Runtime" EventSource counters to capture runtime metrics
/// like GC pause percentage and thread pool queue depth without requiring ETW admin sessions.
/// </summary>
internal sealed class SystemRuntimeCountersListener : EventListener
{
    private readonly ConcurrentDictionary<string, double> _gauges = new();
    private readonly ConcurrentDictionary<string, long> _counts = new();

    public double TimeInGcPercent => _gauges.TryGetValue("time-in-gc", out var v) ? v : 0.0;
    public long ThreadPoolQueueLength => _counts.TryGetValue("threadpool-queue-length", out var v) ? v : 0L;
    public long ThreadPoolCompletedItemsCount => _counts.TryGetValue("threadpool-completed-items-count", out var v) ? v : 0L;

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        // Subscribe only to System.Runtime counters
        if (eventSource?.Name == "System.Runtime")
        {
            EnableEvents(eventSource, EventLevel.LogAlways, EventKeywords.None,
                new Dictionary<string, string?>
                {
                    {"EventCounterIntervalSec", "1"}
                });
        }
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        if (eventData.EventName != "EventCounters" || eventData.Payload is null)
            return;

        foreach (var payloadObj in eventData.Payload)
        {
            if (payloadObj is not IDictionary<string, object> payload)
                continue;

            if (!payload.TryGetValue("Name", out var nameObj) || nameObj is not string name)
                continue;

            // Counters may emit "Mean", "Increment" or "Counter" depending on type
            if (payload.TryGetValue("Mean", out var meanObj) && meanObj is double mean)
            {
                _gauges[name] = mean;
            }
            else if (payload.TryGetValue("Increment", out var incObj) && incObj is double inc)
            {
                _counts[name] = unchecked((long)inc);
            }
            else if (payload.TryGetValue("Counter", out var ctrObj) && ctrObj is double ctr)
            {
                _counts[name] = unchecked((long)ctr);
            }
        }
    }
}

/// <summary>
/// Singleton accessor to the SystemRuntimeCountersListener.
/// </summary>
internal static class RuntimeCounters
{
    private static readonly object _gate = new();
    private static SystemRuntimeCountersListener? _listener;

    public static SystemRuntimeCountersListener Instance
    {
        get
        {
            if (_listener is null)
            {
                lock (_gate)
                {
                    _listener ??= new SystemRuntimeCountersListener();
                }
            }
            return _listener;
        }
    }
}
