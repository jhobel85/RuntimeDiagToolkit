# DiagnosticsToolkit.Generators - Roslyn Source Generator

## Overview

**DiagnosticsToolkit.Generators** is a Roslyn source generator that automatically generates metric collection code for methods decorated with the `[MetricCollector]` attribute. This reduces boilerplate and enables developers to easily add performance monitoring to their code.

The existing [MetricCollector] generator only tracks method execution metrics (timing, call count, exceptions)

## Installation

Add the generator NuGet package as an analyzer to your project:

```xml
<ItemGroup>
  <PackageReference Include="DiagnosticsToolkit.Generators" Version="1.0.0" PrivateAssets="all" />
</ItemGroup>
```

Or reference locally:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/DiagnosticsToolkit.Generators" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```

## Basic Usage

### Step 1: Add the Attribute

Decorate your methods with `[MetricCollector]`:

```csharp
using DiagnosticsToolkit.Generators;

public class DataService
{
    [MetricCollector(Category = "DataAccess")]
    public async Task<List<User>> GetUsersAsync(int count)
    {
        await Task.Delay(100);
        return new List<User> { /* ... */ };
    }
}
```

### Step 2: Access Generated Metrics

The generator creates helper classes with metrics methods:

```csharp
// Call your method normally
var users = await service.GetUsersAsync(10);

// Access auto-generated metrics
var metrics = DataService_GetUsersAsync_Metrics.GetMetrics();
Console.WriteLine($"Call count: {metrics.CallCount}");
Console.WriteLine($"Avg time: {metrics.AverageExecutionTimeMs:F2}ms");
Console.WriteLine($"Exceptions: {metrics.ExceptionCount}");
```

## Attribute Options

### Category
```csharp
[MetricCollector(Category = "DataAccess")]
```
Sets the category name for the metric (e.g., "DataAccess", "Processing", "Network").

### TrackExecutionTime
```csharp
[MetricCollector(TrackExecutionTime = true)]  // Default
```
When enabled, tracks:
- `TotalExecutionTimeMs` - Cumulative execution time
- `AverageExecutionTimeMs` - Average execution time
- `CallCount` - Number of calls

### TrackExceptions
```csharp
[MetricCollector(TrackExceptions = true)]  // Default
```
When enabled, counts exceptions thrown by the method in `ExceptionCount`.

### TrackAllocations
```csharp
[MetricCollector(TrackAllocations = true)]  // Default: false
```
When enabled, tracks memory allocations (planned for future implementation).

## Examples

### Example 1: Simple Method Timing

```csharp
[MetricCollector(Category = "Compute")]
public int Calculate(int input)
{
    var result = 0;
    for (int i = 0; i < input; i++)
        result += i * i;
    return result;
}

// Usage
var result = Calculate(1000);
var metrics = MyClass_Calculate_Metrics.GetMetrics();
Console.WriteLine($"Calculated {metrics.CallCount} times, avg {metrics.AverageExecutionTimeMs}ms");
```

### Example 2: Exception Tracking

```csharp
[MetricCollector(Category = "Validation", TrackExceptions = true)]
public void ValidateUser(User user)
{
    if (user == null)
        throw new ArgumentNullException(nameof(user));
    if (string.IsNullOrEmpty(user.Email))
        throw new ArgumentException("Email required");
}

// Usage
int successCount = 0, failureCount = 0;
for (int i = 0; i < 100; i++)
{
    try
    {
        ValidateUser(GetUser(i));
        successCount++;
    }
    catch { /* ignore */ }
}

var metrics = MyClass_ValidateUser_Metrics.GetMetrics();
Console.WriteLine($"Success: {successCount}, Failures: {metrics.ExceptionCount}");
```

### Example 3: Async Method Tracking

```csharp
[MetricCollector(Category = "Network")]
public async Task<string> FetchDataAsync(string url)
{
    using var client = new HttpClient();
    var response = await client.GetAsync(url);
    return await response.Content.ReadAsStringAsync();
}

// Usage
var data = await FetchDataAsync("https://api.example.com/data");
var metrics = MyService_FetchDataAsync_Metrics.GetMetrics();
Console.WriteLine($"Fetched {metrics.CallCount} times, {metrics.ExceptionCount} errors");
```

### Example 4: Multiple Decorated Methods

```csharp
public class OrderService
{
    [MetricCollector(Category = "Database", TrackExecutionTime = true)]
    public async Task<Order> GetOrderAsync(int orderId)
    {
        // Implementation
    }

    [MetricCollector(Category = "Processing", TrackExecutionTime = true)]
    public decimal CalculateTotal(Order order)
    {
        // Implementation
    }

    [MetricCollector(Category = "Persistence")]
    public void SaveOrder(Order order)
    {
        // Implementation
    }

    // Display all metrics
    public void PrintMetrics()
    {
        var getMetrics = OrderService_GetOrderAsync_Metrics.GetMetrics();
        var calcMetrics = OrderService_CalculateTotal_Metrics.GetMetrics();
        var saveMetrics = OrderService_SaveOrder_Metrics.GetMetrics();

        Console.WriteLine($"GetOrder:    {getMetrics.CallCount} calls, avg {getMetrics.AverageExecutionTimeMs:F2}ms");
        Console.WriteLine($"CalculateTotal: {calcMetrics.CallCount} calls, avg {calcMetrics.AverageExecutionTimeMs:F2}ms");
        Console.WriteLine($"SaveOrder:   {saveMetrics.CallCount} calls");
    }
}
```

## Generated Code Structure

For each decorated method, the generator creates:

1. **Metrics Helper Class** - `ClassName_MethodName_Metrics`
   - Tracks statistics (calls, time, exceptions)
   - Provides `GetMetrics()` method
   - Uses `Interlocked` operations for thread-safety

2. **Metrics Data Class** - `ExecutionMetrics`
   - `Category` - Category name
   - `MethodName` - Method being tracked
   - `CallCount` - Total calls
   - `TotalExecutionTimeMs` - Total time (if enabled)
   - `AverageExecutionTimeMs` - Average time (if enabled)
   - `ExceptionCount` - Exception count (if enabled)

Example generated code:

```csharp
internal static class MyService_GetDataAsync_Metrics
{
    private static int _callCount = 0;
    private static long _totalExecutionTimeMs = 0;
    private static int _exceptionCount = 0;

    public static ExecutionMetrics GetMetrics()
    {
        return new ExecutionMetrics
        {
            Category = "DataAccess",
            MethodName = "GetDataAsync",
            CallCount = _callCount,
            TotalExecutionTimeMs = _totalExecutionTimeMs,
            AverageExecutionTimeMs = _callCount > 0 ? _totalExecutionTimeMs / (decimal)_callCount : 0,
            ExceptionCount = _exceptionCount,
        };
    }

    internal static void RecordSuccess(long executionTimeMs)
    {
        Interlocked.Increment(ref _callCount);
        Interlocked.Add(ref _totalExecutionTimeMs, executionTimeMs);
    }

    internal static void RecordException()
    {
        Interlocked.Increment(ref _exceptionCount);
    }
}
```

## Performance Considerations

- **Zero-cost abstractions** - Generator creates static helper classes with minimal overhead
- **Thread-safe** - Uses `Interlocked` operations for atomic counting
- **No allocations** - Metrics are stored as primitives, no GC pressure
- **Low latency** - Timing measurement uses `Stopwatch` (high-precision)

## Integration with ASP.NET Core

Collect metrics in your services and expose them via an endpoint:

```csharp
public class UserService
{
    [MetricCollector(Category = "Database")]
    public async Task<User> GetUserByIdAsync(int id)
    {
        // Database call
    }
}

// In your controller
[ApiController]
[Route("api/[controller]")]
public class MetricsController : ControllerBase
{
    [HttpGet("service-metrics")]
    public IActionResult GetMetrics()
    {
        return Ok(new
        {
            GetUserById = UserService_GetUserByIdAsync_Metrics.GetMetrics()
        });
    }
}
```

## Limitations and Future Work

- **Wrapping not automatic** - You currently need to call `RecordSuccess()` manually in your code (future versions will use IL weaving)
- **Allocation tracking** - `TrackAllocations = true` is not yet implemented
- **No filtering** - All exceptions are counted equally (future versions may support filtering)

## Sample Project

See [DiagnosticsToolkit.Generators.Sample](../DiagnosticsToolkit.Generators.Sample/) for a complete working example.

## Contributing

See the main [README.md](../README.md) for contribution guidelines.
