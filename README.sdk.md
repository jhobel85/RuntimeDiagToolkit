# SDK Pinning Overview

This repository uses per-folder `global.json` files to pin .NET SDK versions for consistent local and CI builds:

 - Root (`global.json`): Pins .NET SDK 9.x for core library, tests, and benchmarks.
- `DiagnosticsToolkit.Maui/global.json`: Pins .NET SDK 10.x for the MAUI library.
- `DiagnosticsToolkit.Maui.Sample/global.json`: Pins .NET SDK 10.x for the MAUI sample app.

CI behavior:
- Cross-platform jobs install .NET 8.x and build core and tests only.
- The `maui-macos` job installs both 8.x and 10.x SDKs. MAUI steps run with `working-directory: DiagnosticsToolkit.Maui.Sample` so the local `global.json` (10.x) is respected for MAUI builds.

Notes:
- JSON does not support comments; pin rationale is documented here instead of inside `global.json`.
- Workloads are installed per job: `ios`, `maccatalyst`, `android` for MAUI; `wasm-tools` for other jobs.
