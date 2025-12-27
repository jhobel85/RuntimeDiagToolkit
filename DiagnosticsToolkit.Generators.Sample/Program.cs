using System;
using DiagnosticsToolkit.Generators.Sample;

var service = new DataProcessingService();

Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
Console.WriteLine("║   ROSLYN SOURCE GENERATOR - METRIC COLLECTOR SAMPLE        ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
Console.WriteLine();

// Test successful query
Console.WriteLine("Test 1: Successful database queries");
Console.WriteLine("──────────────────────────────────────────────────────────────");
for (int i = 0; i < 3; i++)
{
    var result = await service.QueryDatabaseAsync($"SELECT * FROM users WHERE id = {i}");
    Console.WriteLine($"✅ Query {i + 1}: Retrieved {result.Count} rows");
}

Console.WriteLine();
Console.WriteLine("Test 2: Query with error");
Console.WriteLine("──────────────────────────────────────────────────────────────");
try
{
    var result = await service.QueryDatabaseAsync("SELECT * FROM error_table");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Expected error caught: {ex.Message}");
}

Console.WriteLine();
Console.WriteLine("Test 3: Data processing");
Console.WriteLine("──────────────────────────────────────────────────────────────");
var data = Enumerable.Range(1, 20).ToList();
service.ProcessData(data);
Console.WriteLine($"✅ Processed {data.Count} items");

Console.WriteLine();
Console.WriteLine("Test 4: Data transformation");
Console.WriteLine("──────────────────────────────────────────────────────────────");
var transformed = service.TransformData(data);
Console.WriteLine($"✅ Transformed {transformed.Count} items");

Console.WriteLine();
Console.WriteLine();
Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
Console.WriteLine("║                    COLLECTED METRICS                       ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
Console.WriteLine();

var queryMetrics = DataProcessingService.GetQueryDatabaseMetrics();
Console.WriteLine($"📊 QueryDatabaseAsync");
Console.WriteLine($"   Calls:           {queryMetrics.CallCount}");
Console.WriteLine($"   Exceptions:      {queryMetrics.ExceptionCount}");
Console.WriteLine($"   Total Time:      {queryMetrics.TotalExecutionTimeMs} ms");
Console.WriteLine($"   Average Time:    {queryMetrics.AverageExecutionTimeMs:F2} ms");
Console.WriteLine();

var processMetrics = DataProcessingService.GetProcessDataMetrics();
Console.WriteLine($"📊 ProcessData");
Console.WriteLine($"   Calls:           {processMetrics.CallCount}");
Console.WriteLine($"   Exceptions:      {processMetrics.ExceptionCount}");
Console.WriteLine();

var transformMetrics = DataProcessingService.GetTransformDataMetrics();
Console.WriteLine($"📊 TransformData");
Console.WriteLine($"   Calls:           {transformMetrics.CallCount}");
Console.WriteLine($"   Total Time:      {transformMetrics.TotalExecutionTimeMs} ms");
Console.WriteLine($"   Average Time:    {transformMetrics.AverageExecutionTimeMs:F2} ms");

Console.WriteLine();
Console.WriteLine("✨ Source generator automatically tracked all metrics!");
