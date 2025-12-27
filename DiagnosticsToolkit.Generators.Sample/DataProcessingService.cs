using DiagnosticsToolkit.Generators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiagnosticsToolkit.Generators.Sample;

/// <summary>
/// Example service demonstrating source-generated metric collection helpers.
/// 
/// Decorate methods with [MetricCollector] to generate helper classes.
/// Then manually call the generated helper methods to record metrics.
/// </summary>
public class DataProcessingService
{
    private static Random _random = new Random();

    /// <summary>
    /// Simulates a database query with metric collection.
    /// Decorated with [MetricCollector] to generate: DataProcessingService_QueryDatabaseAsync_Metrics
    /// </summary>
    [MetricCollector]
    public async Task<List<int>> QueryDatabaseAsync(string query)
    {
        return await DataProcessingService_QueryDatabaseAsync_Metrics.MeasureAsync(async () =>
        {
            await Task.Delay(_random.Next(10, 100));

            if (query.Contains("error", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Invalid query: {query}");

            return Enumerable.Range(1, 10).ToList();
        });
    }

    /// <summary>
    /// Processes data with metric collection.
    /// Decorated with [MetricCollector] to generate: DataProcessingService_ProcessData_Metrics
    /// </summary>
    [MetricCollector]
    public void ProcessData(List<int> data)
    {
        DataProcessingService_ProcessData_Metrics.Measure(() =>
        {
            foreach (var item in data)
            {
                var result = Math.Sqrt(item) * 100;
            }
        });
    }

    /// <summary>
    /// Transforms data with metric collection.
    /// Decorated with [MetricCollector] to generate: DataProcessingService_TransformData_Metrics
    /// </summary>
    [MetricCollector]
    public List<string> TransformData(List<int> items)
    {
        return DataProcessingService_TransformData_Metrics.Measure(() =>
        {
            return items.Select(i => $"Item_{i}").ToList();
        });
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
