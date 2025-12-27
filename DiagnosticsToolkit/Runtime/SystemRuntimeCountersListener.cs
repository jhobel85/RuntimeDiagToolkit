using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;

namespace DiagnosticsToolkit.Runtime;

/// <summary>
/// Listens to the "System.Runtime" EventSource counters to capture runtime metrics
/// like GC pause percentage and thread pool queue depth without requiring ETW admin sessions.
/// </summary>
internal sealed class SystemRuntimeCountersListener : EventListener
{
    // Volatile fields to avoid dictionary allocations and locks; updated from EventListener thread.
    private double _timeInGcPercent;
    private long _threadPoolQueueLength;
    private long _threadPoolCompletedItemsCount;

    public double TimeInGcPercent => Volatile.Read(ref _timeInGcPercent);
    public long ThreadPoolQueueLength => Volatile.Read(ref _threadPoolQueueLength);
    public long ThreadPoolCompletedItemsCount => Volatile.Read(ref _threadPoolCompletedItemsCount);

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
                switch (name)
                {
                    case "time-in-gc":
                        Volatile.Write(ref _timeInGcPercent, mean);
                        break;
                    case "threadpool-queue-length":
                        Volatile.Write(ref _threadPoolQueueLength, unchecked((long)mean));
                        break;
                }
            }
            else if (payload.TryGetValue("Increment", out var incObj) && incObj is double inc)
            {
                if (name == "threadpool-completed-items-count")
                {
                    Volatile.Write(ref _threadPoolCompletedItemsCount, unchecked((long)inc));
                }
            }
            else if (payload.TryGetValue("Counter", out var ctrObj) && ctrObj is double ctr)
            {
                if (name == "threadpool-completed-items-count")
                {
                    Volatile.Write(ref _threadPoolCompletedItemsCount, unchecked((long)ctr));
                }
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
