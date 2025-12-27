using DiagnosticsToolkit.Generators;
using System;
using System.Collections.Generic;
using System.Linq;

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
    public async System.Threading.Tasks.Task<List<int>> QueryDatabaseAsync(string query)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await System.Threading.Tasks.Task.Delay(_random.Next(10, 100));

            if (query.Contains("error", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Invalid query: {query}");

            var result = Enumerable.Range(1, 10).ToList();
            
            stopwatch.Stop();
            // Use the generated helper class
            DataProcessingService_QueryDatabaseAsync_Metrics.RecordSuccess(stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch
        {
            // Use the generated helper class
            DataProcessingService_QueryDatabaseAsync_Metrics.RecordException();
            throw;
        }
    }

    /// <summary>
    /// Processes data with metric collection.
    /// Decorated with [MetricCollector] to generate: DataProcessingService_ProcessData_Metrics
    /// </summary>
    [MetricCollector]
    public void ProcessData(List<int> data)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            foreach (var item in data)
            {
                var result = Math.Sqrt(item) * 100;
            }
            
            stopwatch.Stop();
            // Use the generated helper class
            DataProcessingService_ProcessData_Metrics.RecordSuccess(stopwatch.ElapsedMilliseconds);
        }
        catch
        {
            // Use the generated helper class
            DataProcessingService_ProcessData_Metrics.RecordException();
            throw;
        }
    }

    /// <summary>
    /// Transforms data with metric collection.
    /// Decorated with [MetricCollector] to generate: DataProcessingService_TransformData_Metrics
    /// </summary>
    [MetricCollector]
    public List<string> TransformData(List<int> items)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var result = items.Select(i => $"Item_{i}").ToList();
            
            stopwatch.Stop();
            // Use the generated helper class
            DataProcessingService_TransformData_Metrics.RecordSuccess(stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch
        {
            // Use the generated helper class
            DataProcessingService_TransformData_Metrics.RecordException();
            throw;
        }
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
