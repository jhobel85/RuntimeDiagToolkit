namespace DiagnosticsToolkit.Models;

/// <summary>
/// Represents memory usage metrics at a point in time.
/// </summary>
public readonly record struct MemorySnapshot
{
    /// <summary>
    /// Total physical memory available on the system (in bytes).
    /// </summary>
    public long TotalSystemMemoryBytes { get; init; }

    /// <summary>
    /// Available free memory on the system (in bytes).
    /// </summary>
    public long AvailableSystemMemoryBytes { get; init; }

    /// <summary>
    /// Memory used by the current process (in bytes).
    /// </summary>
    public long ProcessWorkingSetBytes { get; init; }

    /// <summary>
    /// Private memory used by the process (in bytes).
    /// </summary>
    public long ProcessPrivateMemoryBytes { get; init; }

    /// <summary>
    /// Managed heap size (in bytes).
    /// </summary>
    public long ManagedHeapBytes { get; init; }

    /// <summary>
    /// Virtual memory used by the process (in bytes).
    /// </summary>
    public long ProcessVirtualMemoryBytes { get; init; }

    /// <summary>
    /// Memory pressure level (0-100, where 0 is no pressure and 100 is critical).
    /// Platform-specific interpretation; useful on mobile platforms.
    /// </summary>
    public int MemoryPressurePercentage { get; init; }

    /// <summary>
    /// Timestamp when the metric was collected.
    /// </summary>
    public DateTimeOffset CollectedAt { get; init; }
}
