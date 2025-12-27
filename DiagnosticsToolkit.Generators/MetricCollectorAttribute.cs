using System;

namespace DiagnosticsToolkit.Generators;

/// <summary>
/// Marks a method for automatic metric collection code generation.
/// The source generator will wrap the method with timing, exception handling, and metric recording.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class MetricCollectorAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the category name for the metric (e.g., "DataAccess", "Processing").
    /// </summary>
    public string Category { get; set; } = "General";

    /// <summary>
    /// Gets or sets whether to track execution time.
    /// </summary>
    public bool TrackExecutionTime { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to track exceptions.
    /// </summary>
    public bool TrackExceptions { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to track memory allocations.
    /// </summary>
    public bool TrackAllocations { get; set; } = false;
}

/// <summary>
/// Represents execution metrics collected for a method.
/// </summary>
public class MethodMetrics
{
    /// <summary>Gets the method name.</summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>Gets the total number of calls.</summary>
    public int CallCount { get; set; }

    /// <summary>Gets total execution time in milliseconds.</summary>
    public long TotalExecutionTimeMs { get; set; }

    /// <summary>Gets average execution time in milliseconds.</summary>
    public decimal AverageExecutionTimeMs { get; set; }

    /// <summary>Gets the number of exceptions.</summary>
    public int ExceptionCount { get; set; }
}
