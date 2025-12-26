namespace DiagnosticsToolkit.Models;

/// <summary>
/// Represents thread pool statistics.
/*
ThreadPool Stats: Indicates thread usage, queue length, and concurrency behavior. Useful for:
- Diagnosing thread starvation or deadlocks
- Optimizing parallel workloads
- Ensuring responsive background processing
*/
/// </summary>
public readonly record struct ThreadPoolStats
{
    /// <summary>
    /// Total number of worker threads.
    /// </summary>
    public int WorkerThreadCount { get; init; }

    /// <summary>
    /// Number of available worker threads.
    /// </summary>
    public int AvailableWorkerThreads { get; init; }

    /// <summary>
    /// Total number of I/O threads.
    /// </summary>
    public int IoThreadCount { get; init; }

    /// <summary>
    /// Number of available I/O threads.
    /// </summary>
    public int AvailableIoThreads { get; init; }

    /// <summary>
    /// Number of queued work items waiting for a worker thread.
    /// </summary>
    public long QueuedWorkItemCount { get; init; }

    /// <summary>
    /// Number of completed work items.
    /// </summary>
    public long CompletedWorkItemCount { get; init; }

    /// <summary>
    /// Minimum worker threads configured.
    /// </summary>
    public int MinWorkerThreads { get; init; }

    /// <summary>
    /// Maximum worker threads configured.
    /// </summary>
    public int MaxWorkerThreads { get; init; }

    /// <summary>
    /// Timestamp when the metric was collected.
    /// </summary>
    public DateTimeOffset CollectedAt { get; init; }
}
