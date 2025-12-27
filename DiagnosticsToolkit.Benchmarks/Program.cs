using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using DiagnosticsToolkit.Providers;

var config = ManualConfig.Create(DefaultConfig.Instance)
	.AddJob(Job.ShortRun.WithWarmupCount(1).WithIterationCount(5))
	.AddExporter(JsonExporter.Full);

BenchmarkRunner.Run<RuntimeMetricsBenchmarks>(config);

public class RuntimeMetricsBenchmarks
{
	private readonly IRuntimeMetricsProvider _provider = RuntimeMetricsProviderFactory.Create();

	[Benchmark]
	public async Task CpuUsage()
	{
		_ = await _provider.GetCpuUsageAsync();
	}

	[Benchmark]
	public async Task MemorySnapshot()
	{
		_ = await _provider.GetMemorySnapshotAsync();
	}

	[Benchmark]
	public async Task GcStats()
	{
		_ = await _provider.GetGcStatsAsync();
	}

	[Benchmark]
	public async Task ThreadPoolStats()
	{
		_ = await _provider.GetThreadPoolStatsAsync();
	}
}
