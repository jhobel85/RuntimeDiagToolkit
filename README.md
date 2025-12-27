# .NET Cross‑Platform Runtime Metrics \& AI‑Assisted Diagnostics Toolkit



**## Project Goal**

Build a cross‑platform diagnostics toolkit that exposes a unified API for collecting runtime metrics (CPU, memory, GC, thread pool, I/O) across Windows, Linux, macOS, iOS, and Android.  

The toolkit must include an AI‑assisted developer workflow that helps identify performance issues and suggests optimizations.



---



**## Core Requirements**



\### Cross‑Platform Library

\- Implement a `.NET 8` class library targeting:

&nbsp; - `net8.0`

&nbsp; - `net8.0-android`

&nbsp; - `net8.0-ios`

&nbsp; - `net8.0-maccatalyst`

&nbsp; - `net8.0-windows`

\- Provide a unified `IRuntimeMetricsProvider` interface.

\- Implement platform‑specific providers:

&nbsp; - \*\*Windows:\*\* ETW + `System.Diagnostics.Process`

&nbsp; - \*\*Linux:\*\* `/proc` filesystem + cgroup v2 metrics

&nbsp; - \*\*macOS/iOS:\*\* unified logging + native interop for memory pressure

&nbsp; - \*\*Android:\*\* ADB‑compatible metrics + Java interop for CPU/memory



\### API Design \& Developer Experience

\- Expose a simple, discoverable API:

&nbsp; - `GetCpuUsageAsync()`

&nbsp; - `GetMemorySnapshotAsync()`

&nbsp; - `GetGcStats()`

&nbsp; - `GetThreadPoolStats()`

\- Provide extension packages:

&nbsp; - `DiagnosticsToolkit.AspNetCore`

&nbsp; - `DiagnosticsToolkit.Maui`

\- Add a Roslyn source generator to:

&nbsp; - Auto‑generate metric collectors

&nbsp; - Reduce boilerplate for developers



\### Performance \& Optimization

\- Ensure all APIs are:

&nbsp; - Allocation‑free in steady state

&nbsp; - Span‑based where possible

&nbsp; - Safe for high‑throughput cloud workloads

\- Add BenchmarkDotNet benchmarks for:

&nbsp; - CPU sampling

&nbsp; - Memory snapshotting

&nbsp; - GC stats retrieval

\- Optimize mobile implementations for:

&nbsp; - Battery usage

&nbsp; - GC pressure

&nbsp; - Background execution limits



---



**## AI‑Assisted Engineering Features**



\### AI‑Driven Diagnostics Analyzer

\- Build a lightweight analyzer that:

&nbsp; - Consumes collected metrics

&nbsp; - Detects common performance issues (e.g., thread pool starvation, GC thrashing)

&nbsp; - Generates human‑readable suggestions using an AI model (local or cloud)

\- Provide a CLI tool:

&nbsp; - `diagnostics-ai analyze --input metrics.json`

&nbsp; - Outputs recommended fixes and code hotspots



\### AI‑Generated Developer Guidance

\- Auto‑generate:

&nbsp; - Sample code snippets

&nbsp; - Integration steps for ASP.NET Core and MAUI

&nbsp; - Troubleshooting guides based on detected patterns



---



**## Cross‑Platform Testing \& Validation**

\- Create automated tests for:

&nbsp; - Windows, Linux, macOS (GitHub Actions)

&nbsp; - Android/iOS (MAUI test harness)

\- Validate:

&nbsp; - Metric accuracy across platforms

&nbsp; - API consistency

&nbsp; - Performance regressions (CI benchmark gate)



---



**## Open‑Source Collaboration**

\- Host the project in a public GitHub repo.

\- Provide:

&nbsp; - Contribution guidelines

&nbsp; - API documentation

&nbsp; - Issue templates

\- Participate in:

&nbsp; - API reviews

&nbsp; - Community discussions

&nbsp; - Cross‑team design syncs

---

**## Deliverables**

\- Cross‑platform `.NET` diagnostics library

\- Source generator package

\- AI‑assisted diagnostics CLI

\- Benchmarks + performance reports

\- GitHub Actions CI pipeline

\- Documentation + samples

**## Planning and project overview**
 [x] Scaffold (skeleton) .NET 8 multi-target library
 [x] Define IRuntimeMetricsProvider API
 [x] Add core API models (CPU, Memory, GC, ThreadPool)
 [x] Build platform dispatcher (DI-friendly factory)
 [x] Create DiagnosticsToolkit.AspNetCore package
 [x] Implement Windows metrics provider (ETW + Process)
 [x] Implement Linux metrics provider (/proc + cgroup v2)
 [x] Implement macOS/iOS provider (process + sysctl + vm_statistics64 for memory pressure)
 [x] Implement Android metrics provider (/proc sampling + runtime counters)
 [x] Create DiagnosticsToolkit.Maui package
 [x] BenchmarkDotNet suite (CPU, Memory, GC, ThreadPool)
 [x] Cross-platform unit tests (xUnit)
 [x] GitHub Actions CI workflow (Windows, Linux, macOS)
 [x] Roslyn source generator for collectors
 [x] Performance hardening (allocation-free ValueStopwatch, stack-based span readers, volatile fields instead of ConcurrentDictionary)
 [x] Optimize mobile implementations (battery, GC, background)
 [x] AI diagnostics analyzer (issue detection + suggestions)
 [x] CLI: diagnostics-ai analyze --input metrics.json
 [x] AI-generated guidance (samples, integrations, troubleshooting)
 [] CI benchmark regression gate
 [] Open-source repo setup (guides, docs, templates)
 [] Documentation & samples
 [] Deliverables packaging

---

## Project summary

- Cross-platform (Win, Linux, macOS, Android, iOS, ..) diagnostics library with unified `IRuntimeMetricsProvider` and platform-specific providers. 
UseCases:
- Performance Monitoring & Diagnostics: ASP.NET Core package exposes a minimal diagnostics endpoint for quick integration.
-  Benchmarking & Profiling: Benchmark suite (BenchmarkDotNet) measures call overhead for CPU, memory, GC, and thread pool metrics. Helps developers measure runtime behavior under load and identify bottlenecks without significant overhead.
- Production Health Checks: Lightweight enough to run periodically in live systems for telemetry and alerting without impacting performance. 

## ASP.NET Core Samples

### Minimal API Sample

- Project: [DiagnosticsToolkit.AspNetCore.Sample.API](DiagnosticsToolkit.AspNetCore.Sample.API/Program.cs)
- Use Case: Lightweight, high-throughput scenarios with minimal API endpoints.
- Run locally (Windows, Linux or macOS):
	- ```bash
		cd DiagnosticsToolkit.AspNetCore.Sample.API
		dotnet run
		```
- Endpoints:
	- Root: `/` → simple health text
	- Metrics: `/_diagnostics/runtime` → CPU, memory, GC, thread pool metrics as JSON
- Publish on Android:
	- ```bash
		dotnet publish DiagnosticsToolkit.AspNetCore.Sample.API/DiagnosticsToolkit.AspNetCore.Sample.API.csproj -f net8.0-android -c Release
		```

### MVC Sample

- Project: [DiagnosticsToolkit.AspNetCore.Sample.MVC](DiagnosticsToolkit.AspNetCore.Sample.MVC/Controllers/HomeController.cs)
- Use Case: Traditional MVC applications with UI dashboard for viewing runtime metrics.
- Run locally (Windows, Linux or macOS):
	- ```bash
		cd DiagnosticsToolkit.AspNetCore.Sample.MVC
		dotnet run
		```
- View: Open `https://localhost:7048` in your browser to see the diagnostics dashboard.
- Features:
	- Real-time CPU, memory, GC, and thread pool metrics displayed in a responsive Bootstrap dashboard.
	- Home page shows live system diagnostics.
	- Privacy page for standard policies.


## .NET MAUI

- Package: DiagnosticsToolkit.Maui
- Register in `MauiProgram.cs`:
	- ```csharp
		var builder = MauiApp.CreateBuilder();
		builder
		    .UseMauiApp<App>()
				//Optional: Base interval while app is foreground; Android adapts in background 
			.UseDiagnosticsToolkit(o => o.BaseInterval = TimeSpan.FromMilliseconds(250));
		return builder.Build();
		```
- Targets: net8.0-android, net8.0-ios, net8.0-maccatalyst

### Mobile Sampling Controls

- Android and Apple providers expose sampling controls for battery-friendly telemetry:
	- Foreground/background: call `OnAppForegrounded()` and `OnAppBackgrounded()` to pause or throttle sampling when app moves state.
	- Sampling interval: `SetSamplingInterval(TimeSpan.FromMilliseconds(250))` configures the base cadence; Android adapts with exponential backoff while backgrounded (up to ~5s) and resets on foreground.
	- Providers return the last cached snapshot when sampling is paused or within the configured interval, avoiding unnecessary work.

- Lifecycle events are hooked automatically:
	- Android: Foreground on Start/Resume, background on Pause/Stop
	- iOS/Mac Catalyst: Foreground on WillEnterForeground/Activated, background on DidEnterBackground/ResignActivation


### MAUI Sample App

- Project: [DiagnosticsToolkit.Maui.Sample](DiagnosticsToolkit.Maui.Sample/)
- Run (requires MAUI workloads):
	- Install workloads once:
		```bash
		dotnet workload restore
		```
	- Android emulator/device:
		```bash
		dotnet build DiagnosticsToolkit.Maui.Sample/DiagnosticsToolkit.Maui.Sample.csproj -f net8.0-android -c Debug
		```
	- iOS (on macOS):
		```bash
		dotnet build DiagnosticsToolkit.Maui.Sample/DiagnosticsToolkit.Maui.Sample.csproj -f net8.0-ios -c Debug
		```
	- Mac Catalyst:
		```bash
		dotnet build DiagnosticsToolkit.Maui.Sample/DiagnosticsToolkit.Maui.Sample.csproj -f net8.0-maccatalyst -c Debug
		```

The sample demonstrates:
- Lifecycle-driven sampling throttling
- Configurable base interval via `UseDiagnosticsToolkit(o => o.BaseInterval = TimeSpan.FromMilliseconds(250))`
- Buttons to fetch CPU, Memory, GC, and ThreadPool metrics and display results

## Benchmarking

### Performance Metrics

Run benchmarks:
- On Linux/macOS: use the cross-platform target
	- ```bash
		cd DiagnosticsToolkit.Benchmarks
		dotnet run -c Release -f net8.0
		```
- On Windows: prefer the Windows target to include OS-specific metrics
	- ```bash
		cd DiagnosticsToolkit.Benchmarks
		dotnet run -c Release -f net8.0-windows
		```

Example baseline results (may vary by hardware):

| Method          | Mean            | Error          | StdDev        |
|---------------- |----------------:|---------------:|--------------:|
| CpuUsage        |    30,814.18 ns |   3,850.151 ns |    999.872 ns |
| MemorySnapshot  | 4,381,964.38 ns | 369,675.287 ns | 96,003.498 ns |
| GcStats         |       120.67 ns |       5.626 ns |      0.871 ns |
| ThreadPoolStats |        80.54 ns |      13.930 ns |      3.618 ns |

**Performance notes:**
- **CpuUsage**: ~30.8 µs (moderate, due to sampling)
- **MemorySnapshot**: ~4.38 ms (heavyweight, avoid in tight loops for production)
- **GcStats**: ~120 ns (extremely lightweight, safe for frequent collection)
- **ThreadPoolStats**: ~80 ns (extremely lightweight, safe for frequent collection)

## Testing

### Unit Tests

Run tests across all platforms:
- ```bash
	dotnet test DiagnosticsToolkit.Tests/DiagnosticsToolkit.Tests.csproj -c Release
	```

Test coverage includes:
- Individual metric collection validation (CPU, Memory, GC, ThreadPool)
- Consistency checks across multiple calls
- Concurrent metric collection stress tests
- Dependency injection registration and initialization
- Model validation and timestamp tracking

###  Continuous Integration

GitHub Actions workflow runs on every push/PR:
- **Platforms:** Windows, Linux, macOS
- **Targets:** .NET 8.0 (cross-platform) and net8.0-windows (Windows-specific)
- **Jobs:**
  - Build and Test: Compiles on all platforms, runs full test suite
  - Benchmarks: Runs performance baseline on each platform
  - Code Quality: Checks for unused imports and enforces code style

View CI status in `.github/workflows/dotnet.yml`.
