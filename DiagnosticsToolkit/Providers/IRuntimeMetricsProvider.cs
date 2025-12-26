namespace DiagnosticsToolkit.Providers;

using DiagnosticsToolkit.Models;

/// <summary>
/// Unified interface for collecting runtime metrics across platforms.
/// Implementations are platform-specific and should be allocation-free in steady state.
/// </summary>
public interface IRuntimeMetricsProvider
{
    /// <summary>
    /// Gets current CPU usage metrics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>CPU usage snapshot.</returns>
    ValueTask<CpuUsage> GetCpuUsageAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current memory metrics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Memory snapshot.</returns>
    ValueTask<MemorySnapshot> GetMemorySnapshotAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets garbage collection statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>GC statistics.</returns>
    ValueTask<GcStats> GetGcStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets thread pool statistics.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Thread pool statistics.</returns>
    ValueTask<ThreadPoolStats> GetThreadPoolStatsAsync(CancellationToken cancellationToken = default);
}
