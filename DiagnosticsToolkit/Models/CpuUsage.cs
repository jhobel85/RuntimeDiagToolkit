namespace DiagnosticsToolkit.Models;

/// <summary>
/// Represents CPU usage metrics collected at a point in time.
/// Designed to be allocation-free and span-friendly.
/// </summary>
public readonly record struct CpuUsage
{
    /// <summary>
    /// Percentage of CPU usage (0-100 per core, or aggregated).
    /// </summary>
    public double PercentageUsed { get; init; }

    /// <summary>
    /// Total CPU time in milliseconds.
    /// </summary>
    public long TotalProcessorTimeMs { get; init; }

    /// <summary>
    /// User-mode CPU time in milliseconds.
    /// </summary>
    public long UserModeTimeMs { get; init; }

    /// <summary>
    /// Kernel-mode CPU time in milliseconds.
    /// </summary>
    public long KernelModeTimeMs { get; init; }

    /// <summary>
    /// Number of logical processors.
    /// </summary>
    public int ProcessorCount { get; init; }

    /// <summary>
    /// Timestamp when the metric was collected.
    /// </summary>
    public DateTimeOffset CollectedAt { get; init; }
}
