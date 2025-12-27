using DiagnosticsToolkit.Generators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiagnosticsToolkit.Generators.Sample;

/// <summary>
/// Example service that uses the [MetricCollector] attribute to automatically
/// generate metric collection code for its methods.
/// </summary>
public class DataProcessingService
{
    private static Random _random = new Random();

    /// <summary>
    /// Simulates a database query with automatic metric collection.
    /// The source generator will wrap this method with timing and exception tracking.
    /// </summary>
    [MetricCollector]
    public async System.Threading.Tasks.Task<List<int>> QueryDatabaseAsync(string query)
    {
        // Simulate database delay
        await System.Threading.Tasks.Task.Delay(_random.Next(10, 100));

        if (query.Contains("error", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Invalid query: {query}");

        return Enumerable.Range(1, 10).ToList();
    }

    /// <summary>
    /// Processes data with automatic metric collection.
    /// </summary>
    [MetricCollector]
    public void ProcessData(List<int> data)
    {
        // Simulate processing
        foreach (var item in data)
        {
            var result = Math.Sqrt(item) * 100;
            // Do something with result
        }
    }

    /// <summary>
    /// Transforms data with automatic timing metric collection.
    /// </summary>
    [MetricCollector]
    public List<string> TransformData(List<int> items)
    {
        return items.Select(i => $"Item_{i}").ToList();
    }

    /// <summary>
    /// Gets the collected metrics for a specific method.
    /// </summary>
    public static MethodMetrics GetQueryDatabaseMetrics()
    {
        return DataProcessingService_QueryDatabaseAsync_Metrics.GetMetrics();
    }

    public static MethodMetrics GetProcessDataMetrics()
    {
        return DataProcessingService_ProcessData_Metrics.GetMetrics();
    }

    public static MethodMetrics GetTransformDataMetrics()
    {
        return DataProcessingService_TransformData_Metrics.GetMetrics();
    }
}
