# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-12-27

### Added
- **Core Library**: Cross-platform `.NET 8` diagnostics library with unified `IRuntimeMetricsProvider` interface
  - Platform-specific providers: Windows (ETW), Linux (/proc), macOS/iOS (sysctl), Android (/proc sampling)
  - Runtime metrics: CPU usage, memory snapshots, GC stats, thread pool statistics
  - Allocation-free steady-state APIs with span-based readers
- **Extensions**:
  - `DiagnosticsToolkit.AspNetCore`: Minimal API endpoint for runtime diagnostics
  - `DiagnosticsToolkit.Maui`: Mobile integration with lifecycle hooks and adaptive battery-aware sampling
- **Roslyn Source Generator**: Auto-generates metric collector helpers with execution timing and exception tracking
- **AI Diagnostics CLI**: `diagnostics-ai analyze --input metrics.json` for issue detection and recommendations
- **Performance**:
  - Benchmark suite (BenchmarkDotNet) for CPU, memory, GC, thread pool metrics
  - CI benchmark regression gate (fail on > 10% regression)
  - Mobile optimization: exponential backoff sampling on Android, cached snapshots
- **Samples**:
  - ASP.NET Core Minimal API and MVC dashboards
  - .NET MAUI sample with auto-refresh and background simulation
- **Testing**: Cross-platform xUnit tests and GitHub Actions CI (Windows, Linux, macOS)

### Performance Notes
- **CpuUsage**: ~30.8 µs (moderate, due to sampling)
- **MemorySnapshot**: ~4.38 ms (heavyweight, avoid in tight loops)
- **GcStats**: ~120 ns (safe for frequent collection)
- **ThreadPoolStats**: ~80 ns (safe for frequent collection)

## [Unreleased]

### Planned
- Cgroup v2 metrics on Linux
- iOS background execution limits detection
- AI model fine-tuning for custom workload patterns
