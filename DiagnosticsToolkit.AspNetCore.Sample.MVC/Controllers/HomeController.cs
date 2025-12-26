using DiagnosticsToolkit;
using DiagnosticsToolkit.Providers;
using DiagnosticsToolkit.AspNetCore.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace DiagnosticsToolkit.AspNetCore.Sample.MVC.Controllers;

public class HomeController : Controller
{
    private readonly IRuntimeMetricsProvider _metricsProvider;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IRuntimeMetricsProvider metricsProvider, ILogger<HomeController> logger)
    {
        _metricsProvider = metricsProvider;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var cpu = await _metricsProvider.GetCpuUsageAsync();
            var memory = await _metricsProvider.GetMemorySnapshotAsync();
            var gc = await _metricsProvider.GetGcStatsAsync();
            var threadPool = await _metricsProvider.GetThreadPoolStatsAsync();

            var model = new DiagnosticsViewModel
            {
                CpuUsagePercent = cpu.PercentageUsed,
                ProcessorCount = cpu.ProcessorCount,
                MemoryUsedMB = memory.ProcessWorkingSetBytes / (1024.0 * 1024.0),
                MemoryTotalMB = memory.TotalSystemMemoryBytes / (1024.0 * 1024.0),
                MemoryPressure = $"{memory.MemoryPressurePercentage}%",
                GcTotalPauseMsPercent = gc.TotalGcPauseMsPercentage,
                HeapFragmentationPercent = gc.HeapFragmentationPercentage,
                Gen0Collections = gc.Gen0CollectionCount,
                Gen1Collections = gc.Gen1CollectionCount,
                Gen2Collections = gc.Gen2CollectionCount,
                ThreadPoolWorkerThreads = threadPool.WorkerThreadCount,
                ThreadPoolIOThreads = threadPool.IoThreadCount,
                ThreadPoolQueuedWorkItems = threadPool.QueuedWorkItemCount,
                ThreadPoolCompletedItems = threadPool.CompletedWorkItemCount
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting diagnostics metrics");
            return View("Error");
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }
}

public class DiagnosticsViewModel
{
    public double CpuUsagePercent { get; set; }
    public int ProcessorCount { get; set; }
    public double MemoryUsedMB { get; set; }
    public double MemoryTotalMB { get; set; }
    public string MemoryPressure { get; set; } = "Unknown";
    public double GcTotalPauseMsPercent { get; set; }
    public double HeapFragmentationPercent { get; set; }
    public long Gen0Collections { get; set; }
    public long Gen1Collections { get; set; }
    public long Gen2Collections { get; set; }
    public int ThreadPoolWorkerThreads { get; set; }
    public int ThreadPoolIOThreads { get; set; }
    public long ThreadPoolQueuedWorkItems { get; set; }
    public long ThreadPoolCompletedItems { get; set; }
}
